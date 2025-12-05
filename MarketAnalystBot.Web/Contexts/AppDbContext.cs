using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Emit;

public class AppDbContext : DbContext
{
    public DbSet<Tickers> Tickers { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

    }
}

public class Tickers
{
    [Key]
    public long IdTicker {  get; set; }
    public string? CodTicker { get; set; }
    public DateTime DataRegistro { get; set; }
    public decimal? Score { get; set; }
    public string? Logo { get; set; }
    public string? Sector { get; set; }
    public string? Nome { get; set; }
}