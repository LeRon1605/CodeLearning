﻿using API.Models.DTO;
using API.Models.Entity;
using API.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        public UserController(IUnitOfWork unitOfWork, IUserRepository userRepository, IRoleRepository roleRepository)
        {
            _unitOfWork = unitOfWork;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAll()
        {
            return Ok(new
            {
                status = true,
                data = _userRepository.FindAll().Select(user => new UserDTO
                {
                    ID = user.ID,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt                
                })
            }); 
        }   

        [HttpGet("{ID}")]
        [Authorize]
        public IActionResult GetByID(string ID)
        {
            if (string.IsNullOrEmpty(ID))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Invalid ID"
                });
            }
            User user = _userRepository.FindSingle(user => user.ID == ID);
            if (user == null)
            {
                return NotFound(new
                {
                    status = false,
                    message = "Không tìm thấy User"
                });
            }
            else
            {
                return Ok(new
                {
                    status = true,
                    message = "",
                    data = new UserDTO
                    {
                        ID = user.ID,
                        Username = user.Username,
                        Email = user.Email,
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = user.UpdatedAt
                    }
                });
            }
        }

        [HttpPut("{ID}")]
        [Authorize]
        public IActionResult Update(string ID, UserUpdate input)
        {
            string tokenID = User.FindFirst("ID")?.Value;
            if (tokenID == ID || _userRepository.GetUserWithRole(user => user.ID == tokenID).Role.Name == "Admin")
            {
                if (string.IsNullOrEmpty(ID))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid ID"
                    });
                }
                User user = _userRepository.FindSingle(user => user.ID == ID);
                if (user == null)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Không tồn tại user"
                    });
                }
                if (!string.IsNullOrEmpty(input.Username) && _userRepository.isExist(user => user.Username == input.Username))
                {
                    return Conflict(new
                    {
                        status = false,
                        message = "Username đã tồn tại không thể cập nhật"
                    });
                }
                try
                {
                    user.Username = (string.IsNullOrEmpty(input.Username)) ? user.Username : input.Username;
                    user.UpdatedAt = DateTime.Now;
                    _unitOfWork.Commit();
                    return NoContent();
                }
                catch (Exception e)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = e.Message
                    });
                }
            }
            else
            {
                return Forbid();
            }
        }

        [HttpDelete("{ID}")]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string ID)
        {
            if (string.IsNullOrEmpty(ID))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Invalid ID"
                });
            }    
            User user = _userRepository.FindSingle(user => user.ID == ID);
            if (user == null)
            {
                return NotFound(new
                {
                    status = false,
                    message = "Không tồn tại user"
                });
            }
            else
            {
                try
                {
                    _userRepository.Remove(user);
                    _unitOfWork.Commit();
                    return NoContent();
                }
                catch (Exception e)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = e.Message
                    });
                }
            }
        }

        [HttpGet("{ID}/role")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetRole(string ID)
        {
            if (string.IsNullOrEmpty(ID))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Invalid ID"
                });
            }
            User user = _userRepository.GetUserWithRole(user => user.ID == ID);
            if (user == null)
            {
                return NotFound(new
                {
                    status = false,
                    message = "Không tồn tại user"
                });
            }
            else
            {
                return Ok(new
                {
                    status = true,
                    data = new RoleDTO
                    {
                        Name = user.Role.Name,
                        Priority = user.Role.Priority
                    }
                });
            }
        }

        [HttpPut("{ID}/role")]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateRoleOfUser(string ID, RoleUpdate input)
        {
            if (string.IsNullOrEmpty(ID))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Invalid ID"
                });
            }
            User user = _userRepository.GetUserWithRole(user => user.ID == ID);
            if (user == null)
            {
                return NotFound(new
                {
                    status = false,
                    message = "Không tồn tại user"
                });
            }
            else
            {
                Role role = _roleRepository.findByName(input.Name);
                if (role == null)
                {
                    return NotFound(new
                    {
                        status = false,
                        message = "Không tồn tại Role"
                    });
                }
                else
                {
                    try
                    {
                        user.RoleID = role.ID;
                        _userRepository.Update(user);
                        _unitOfWork.Commit();
                        return NoContent();
                    }
                    catch(Exception e)
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                } 
            }
        }
    }
}
