using Inventory2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace Inventory2.Data
{
    internal class SQLiteContext : DbContext
    {
        public virtual DbSet<ImportSettings> ImportSettings { get; set; }
        public virtual DbSet<Product> Product { get; set; }
        
        // Função para buscar produtos paginados
        public List<Product> GetProductsPaged(int pageNumber, int pageSize)
        {
            using (var db = new SQLiteContext())
            {
                return db.Product
                         .OrderBy(p => p.ProductId)
                         .Skip((pageNumber - 1) * pageSize)
                         .Take(pageSize)
                         .ToList();
            }
        }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.ApplyConfigurationsFromAssembly(typeof(SQLiteContext).Assembly);
            base.OnModelCreating(mb);
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var dir = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var db = Path.Combine(dir, "Inventory.sqlite3");
            optionsBuilder.UseSqlite($"Filename = {db}");
            //base.OnConfiguring(optionsBuilder);
        }
        public IDbConnection GetDbConnection() { return Database.GetDbConnection(); }
        public void BeginTransaction(){ Database.BeginTransaction(); }
        public void CommitTransaction() { Database.CommitTransaction(); }
        public void RollbackTransaction() {  Database.RollbackTransaction(); }
        public void Initialize()
        {
            var dir = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var db = Path.Combine(dir, "Inventory.sqlite3");
            if (File.Exists(db)) return;
            Database.EnsureCreated();
        }
    }
}
