﻿using API.Filter;
using API.Helper;
using API.Models.Entity;
using API.Services;
using CodeStudy.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userSerivce;
        private readonly ITokenService _tokenService;
        private readonly IMail _mail;
        public AuthController(IUserService userService, ITokenService tokenSerivce, IMail mail)
        {
            _userSerivce = userService;
            _tokenService = tokenSerivce;
            _mail = mail;
        }
        [HttpPost("register")]
        [ServiceFilter(typeof(ExceptionHandler))]
        public async Task<IActionResult> Register(RegisterUser input)
        {
            if (_userSerivce.Exist(input.Username, input.Email))
            {
                return Conflict(new
                {
                    message = "Tên tài khoản hoặc email đã tồn tại"
                });
            }
            else
            {
                await _userSerivce.Add(input);
                return Ok(new 
                { 
                    message = "Đăng kí tài khoản thành công" 
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginUser input, [FromServices] ILocalAuth authHandler)
        {
            User user = _userSerivce.Login(input.Name, input.Password, authHandler);
            if (user == null)
            {
                return NotFound(new
                {
                    message = "Tài khoản không tồn tại"
                });
            }
            Token token = await _tokenService.GenerateToken(user); 
            return Ok(new 
            {
                message = "Đăng nhập thành công",
                access_token = token.AccessToken,
                refresh_token = token.RefreshToken
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> Refresh(Token token)
        {
            string jwtId = _tokenService.ValidateToken(token.AccessToken);
            if (jwtId == null)
            {
                return BadRequest(new 
                { 
                    message = "Invalid Token"
                });
            }

            Token newToken = await _tokenService.RefreshToken(jwtId, token.RefreshToken);
            if (newToken == null)
            {
                return BadRequest(new 
                { 
                    message = "Invalid refresh_token"    
                });
            }
            else
            {
                return Ok(new
                {
                    access_token = newToken.AccessToken,
                    refresh_token = newToken.RefreshToken
                });
            }
        }

        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword(ForgetPassword input)
        {
            User user = _userSerivce.FindByName(input.Name);
            if (user == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy user"
                });
            }
            string forgetPasswordToken = await _tokenService.GenerateForgerPasswordToken(user);
            string callbackURL = Url.ActionLink("ForgetPassword", "Auth", new { token = forgetPasswordToken, userID = user.ID }, Request.Scheme, Request.Host.Value );
            _ = Task.Run(async () => await _mail.SendMailAsync(user.Email, "Thay đổi mật khẩu", $"<a href={callbackURL}>Bấm vào đây</a>"));
            return Ok();
        }

        [HttpPost(("forget-password/callback"))]
        [QueryConstraint(Key = "token")]
        public async Task<IActionResult> ForgetPassword(string token, string userID, ForgetPasswordSubmit input)
        {
            User user = _userSerivce.FindById(userID);
            if (user == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy user"
                });
            }
            if (await _userSerivce.ChangePassword(user, token, input.Password))
            {
                return Ok();
            }
            return BadRequest();
        }

        [HttpPost("google-signin")]
        [QueryConstraint(Key = "tokenID")]
        public async Task<IActionResult> SignInWithGoogle(string tokenID, [FromServices] IGoogleAuth authHandler)
        {
            GoogleJsonWebSignature.ValidationSettings settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new List<string>() { "49702556741-2isp8q3bmku7qn6m3t37nnjm6rjrimcj.apps.googleusercontent.com" }
            };

            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(tokenID, settings);
            User user = _userSerivce.Login(payload.Email, Constant.PASSWORD_DEFAULT, authHandler);
            if (user == null)
            {
                user = await _userSerivce.AddGoogle(payload.Email, payload.Name);
            }
            Token token = await _tokenService.GenerateToken(user);
            return Ok(new
            {
                token = token.AccessToken,
                refreshToken = token.RefreshToken
            });
        }
    }
}
