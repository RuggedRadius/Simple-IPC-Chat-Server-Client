using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AT2_4.Login
{
    class UserRepository
    {
        List<User> users = new List<User>();
        // Function to add the user to im memory dummy DB
        public void AddUser(User user)
        {
            users.Add(user);
        }
        // Function to retrieve the user based on user id
        public User GetUser(string userid)
        {
            try
            {
                User result = users.Single(u => u.UserId == userid);
                return result;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
