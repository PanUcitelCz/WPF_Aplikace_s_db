using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Aplikace_s_db.Data
{
    public class StudentContext : DbContext
    {
        public DbSet<Student> Students => Set<Student>();


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    "Data Source=(localdb)\\MSSQLLocalDB;" +
                    "Initial Catalog=StudentDbDemo1;" +
                    "Integrated Security=True;" +
                    "TrustServerCertificate=True");
            }
        }

        public void EnsureCreatedAndSeed()
        {
            Database.EnsureCreated();
            SeedIfEmpty();
        }

        public void SeedIfEmpty()
        {
            if (!Students.Any())
            {
                var initial = new List<Student>
                {
                    new Student { FirstName="Jan",   LastName="Novák",    Year=1, Email="jan.novak@example.com" },
                    new Student { FirstName="Petr",  LastName="Svoboda",  Year=2, Email="petr.svoboda@example.com" },
                    new Student { FirstName="Karel", LastName="Černý",    Year=3, Email="karel.cerny@example.com" },
                    new Student { FirstName="Lucie", LastName="Malá",     Year=1, Email="lucie.mala@example.com" },
                    new Student { FirstName="Eva",   LastName="Bílá",     Year=2, Email="eva.bila@example.com" },
                    new Student { FirstName="Adam",  LastName="Zelený",   Year=3, Email="adam.zeleny@example.com" },
                    new Student { FirstName="Tomáš", LastName="Dvořák",   Year=1, Email="tomas.dvorak@example.com" },
                    new Student { FirstName="Marie", LastName="Veselá",   Year=2, Email="marie.vesela@example.com" },
                    new Student { FirstName="Jana",  LastName="Horáková", Year=3, Email="jana.horakova@example.com" },
                    new Student { FirstName="Filip", LastName="Král",     Year=1, Email="filip.kral@example.com" }
                };
                Students.AddRange(initial);
                SaveChanges();
            }
        }
    }
}
