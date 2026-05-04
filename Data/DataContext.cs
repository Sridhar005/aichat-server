using AIChatApp.Models;
using Microsoft.EntityFrameworkCore;
using MyApp.Models;

namespace AIChatApp.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatMessage> Messages { get; set; }
    }
}