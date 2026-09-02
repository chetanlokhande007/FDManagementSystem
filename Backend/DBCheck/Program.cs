using System;
using Npgsql;

class Program
{
    static void Main()
    {
        string connString = "Host=localhost;Port=5432;Database=FDManagementDB;Username=postgres;Password=chetan1328";
        using var conn = new NpgsqlConnection(connString);
        conn.Open();
        
        using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"FDIdentifications\"", conn);
        var count = cmd.ExecuteScalar();
        Console.WriteLine($"FD count: {count}");

        using var cmd2 = new NpgsqlCommand("SELECT COUNT(*) FROM \"Entities\"", conn);
        var count2 = cmd2.ExecuteScalar();
        Console.WriteLine($"Entity count: {count2}");

        using var cmd3 = new NpgsqlCommand("SELECT COUNT(*) FROM \"CounterParties\"", conn);
        var count3 = cmd3.ExecuteScalar();
        Console.WriteLine($"CounterParty count: {count3}");
        
        using var cmd4 = new NpgsqlCommand("SELECT COUNT(*) FROM \"Currencies\"", conn);
        var count4 = cmd4.ExecuteScalar();
        Console.WriteLine($"Currency count: {count4}");
    }
}
