using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.EntityFrameworkCore.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommanderDiscord.Data.Models.Messages
{
    public class MessageContext : DbContext
    {
        public DbSet<Message> Messages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(
                @"Server=(localdb)\mssqllocaldb;Database=Blogging;Integrated Security=True");
        }
    }

    public class Message
    {
        //List<ulong> MentionedUserIds { get; set; }
        //MentionedRoleIds
        //    MentionedChannelIds

        public ulong UniqueId { get; set; }
        public string Username { get; set; }
        public string Content { get; set; }
        public DateTime TimeStmap { get; set; }

    }
}
