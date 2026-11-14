using Microsoft.EntityFrameworkCore;
using Reunioes.Dados.Entidades;

namespace Reunioes.Dados.Context;

public class ReunioesDbContext(DbContextOptions<ReunioesDbContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(room => room.Id);
            entity.Property(room => room.Name).IsRequired().HasMaxLength(120);
            entity.Property(room => room.Floor).IsRequired();
            entity.Property(room => room.Capacity).IsRequired();
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(reservation => reservation.Id);
            entity.Property(reservation => reservation.StartTime).IsRequired();
            entity.Property(reservation => reservation.EndTime).IsRequired();
            entity.Property(reservation => reservation.CreatedAt).IsRequired();
            entity.HasOne(reservation => reservation.Room)
                .WithMany(room => room.Reservations)
                .HasForeignKey(reservation => reservation.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(reservation => new { reservation.RoomId, reservation.StartTime, reservation.EndTime });
        });
    }
}
