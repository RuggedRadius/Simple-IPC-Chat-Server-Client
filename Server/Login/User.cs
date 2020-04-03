using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AT2_4.Login
{
    class User
    {
        public string UserId { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
    }
}
