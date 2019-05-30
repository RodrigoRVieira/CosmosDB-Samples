using CosmosDB.Domain;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace CosmosDB.Repository
{
    public class UserRepository : BaseRepository<User>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configurationSection"></param>
        public UserRepository(IConfigurationSection configurationSection) : base(configurationSection)
        {
            base.Type = "User";
        }
    }
}
