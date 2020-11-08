using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;

namespace EFCview
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext() 
        {
        }
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
    :          base(options)
        {
        }
        public DbSet<Product> Products { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<CompanyProductsGroup> ProductsByCompany { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CompanyProductsGroup>((pc =>
            {
                pc.HasNoKey();
                pc.ToView("View_ProductsByCompany");
            }));
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=querytypesdb;Trusted_Connection=True;");
        }
    }
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }          // цена
        public int TotalCount { get; set; }  // количество единиц данного товара

        public int CompanyId { get; set; }
        public Company Company { get; set; }
    }
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<Product> Products { get; set; } = new List<Product>();
    }
    public class CompanyProductsGroup
    {
        public string CompanyName { get; set; }
        public int ProductCount { get; set; }   // количество товаров
        public int TotalSum { get; set; }       // совокупная цена всех товаров компании
    }
    public class Program
    {
        public static void Main(string[] args)
        {
            using (ApplicationContext db = new ApplicationContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                // создаем представление
                db.Database.ExecuteSqlRaw(@"CREATE VIEW View_ProductsByCompany AS 
                                            SELECT c.Name AS CompanyName, Count(p.Id) AS ProductCount, Sum(p.Price * p.TotalCount) AS TotalSum
                                            FROM Companies c
                                            INNER JOIN Products p on p.CompanyId = c.Id
                                            GROUP BY c.Name");
                // добавляем начальные данные
                Company c1 = new Company { Name = "Apple" };
                Company c2 = new Company { Name = "Samsung" };
                Company c3 = new Company { Name = "Huawei" };
                db.Companies.AddRange(c1, c2, c3);
                Product p1 = new Product { Name = "iPhone X", Company = c1, Price = 70000, TotalCount = 2 };
                Product p2 = new Product { Name = "iPhone 8", Company = c1, Price = 40000, TotalCount = 4 };
                Product p3 = new Product { Name = "Galaxy S9", Company = c2, Price = 42000, TotalCount = 3 };
                Product p4 = new Product { Name = "Galaxy A7", Company = c2, Price = 14000, TotalCount = 5 };
                Product p5 = new Product { Name = "Honor 9", Company = c3, Price = 17000, TotalCount = 7 };
                db.Products.AddRange(p1, p2, p3, p4, p5);
                db.SaveChanges();
            }

            using (ApplicationContext db = new ApplicationContext())
            {
                // обращаемся к представлению
                var companyProducts = db.ProductsByCompany.ToList();
                foreach (var item in companyProducts)
                {
                    Console.WriteLine($"Company: {item.CompanyName} Models: {item.ProductCount} Sum: {item.TotalSum}");
                }
            }
        }
    }
}
