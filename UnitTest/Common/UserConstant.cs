﻿using Data.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest.Common
{
    public static class UserConstant
    {
        public static string ID = "User_ID";
        public static string USERNAME = "Username";

        public static List<User> GetUsers()
        {
            return new List<User>
            {
                new User
                {
                    
                },
                new User
                {
                    
                },
                new User
                {

                }
            };
        }
    }
}
