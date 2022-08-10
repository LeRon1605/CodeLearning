﻿using API.Helper;
using API.Models.DTO;
using API.Models.Entity;
using API.Repository;
using CodeStudy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Services
{
    public interface IUserService
    {
        bool Exist(string username, string email);
        User Login(string name, string password, IAuth auth);
        Task Add(RegisterUser input);
        Task<User> AddGoogle(string email, string name);
        User FindByName(string name);
        User FindById(string id);
        Task<bool> ChangePassword(User user, string token, string password);
        IEnumerable<User> GetAll();
        Task<PagingList<User>> GetPageAsync(int page, int pageSize, string keyword);
        Task<bool> Update(User user, UserUpdate input);
        Task Remove(User user);
        Task<bool> UpdateRole(User user, string role);
        IEnumerable<Submission> GetSubmitOfUser(string Id);

    }
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRoleRepository _roleRepository;
        public UserService(IUserRepository userRepository, IRoleRepository roleRepository, ISubmissionRepository submissionRepository,IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _submissionRepository = submissionRepository;
            _unitOfWork = unitOfWork;
        }
        public async Task Add(RegisterUser input)
        {
            User user = new User
            {
                ID = Guid.NewGuid().ToString(),
                Email = input.Email,
                Password = Encryptor.MD5Hash(input.Password),
                Username = input.Username,
                CreatedAt = DateTime.Now,
                Type = AccountType.Local,
                Role = _roleRepository.FindSingle(role => role.Name == "User")
            };
            await _userRepository.AddAsync(user);
            await _unitOfWork.CommitAsync();
        }

        public async Task<User> AddGoogle(string email, string name)
        {
            User user = new User
            {
                ID = Guid.NewGuid().ToString(),
                Email = email,
                Password = Encryptor.MD5Hash(Constant.PASSWORD_DEFAULT),
                Username = name,
                CreatedAt = DateTime.Now,
                Type = AccountType.Google,
                Role = _roleRepository.FindSingle(role => role.Name == "User")
            };
            await _userRepository.AddAsync(user);
            await _unitOfWork.CommitAsync();
            return user;
        }

        public async Task<bool> ChangePassword(User user, string token, string password)
        {
            if (user.ForgotPasswordToken != null)
            {
                if (user.ForgotPasswordToken == token && user.ForgotPasswordTokenExpireAt >= DateTime.Now)
                {
                    user.Password = Encryptor.MD5Hash(password);
                    user.ForgotPasswordToken = null;
                    user.ForgotPasswordTokenCreatedAt = null;
                    user.ForgotPasswordTokenExpireAt = null;
                    _userRepository.Update(user);
                    await _unitOfWork.CommitAsync();
                    return true;
                }
            }
            return false;
        }

        public bool Exist(string username, string email)
        {
            return _userRepository.isExist(x => x.Username == username || x.Email == email);
        }

        public User FindById(string id)
        {
            return _userRepository.FindSingle(user => user.ID == id);
        }

        public User FindByName(string name)
        {
            return _userRepository.FindSingle(user => (user.Username == name || user.Email == name) && user.Type == AccountType.Local);
        }

        public IEnumerable<User> GetAll()
        {
            return _userRepository.FindAll();
        }

        public async Task<PagingList<User>> GetPageAsync(int page, int pageSize, string keyword)
        {
            return await _userRepository.GetPageAsync(page, pageSize, user => user.Username.Contains(keyword) || user.Email.Contains(keyword));
        }

        public IEnumerable<Submission> GetSubmitOfUser(string Id)
        {
            return _submissionRepository.GetSubmissionsDetail(x => x.UserID == Id);
        }

        public User Login(string name, string password, IAuth auth)
        {
            return _userRepository.GetUserWithRole(auth.Login(name, password));
        }

        public async Task Remove(User user)
        {
            _userRepository.Remove(user);
            await _unitOfWork.CommitAsync();
        }

        public async Task<bool> Update(User user, UserUpdate input)
        {
            if (!string.IsNullOrEmpty(input.Username) && _userRepository.isExist(user => user.Username == input.Username))
            {
                return false;
            }
            user.Username = (string.IsNullOrEmpty(input.Username)) ? user.Username : input.Username;
            user.UpdatedAt = DateTime.Now;
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<bool> UpdateRole(User user, string role)
        {
            Role entity = _roleRepository.findByName(role);
            if (entity == null)
            {
                return false;
            }
            else
            {
                user.RoleID = entity.ID;
                _userRepository.Update(user);
                await _unitOfWork.CommitAsync();
                return true;
            }
        }
    }
}
