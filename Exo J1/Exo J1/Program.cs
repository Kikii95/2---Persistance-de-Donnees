using System.Text.Json;
using Microsoft.EntityFrameworkCore;

public class InventoryItem
{
    public int Id { get; set; }  
    public required string Name { get; set; }
    public int Quantity { get; set; } = 0;
    public string? Category { get; set; }
    public DateTime LastModified { get; set; } = DateTime.Now;
    public int PlayerId { get; set; } = 1;


    public override string ToString()
    {
        var category = !string.IsNullOrEmpty(Category) ? $" ({Category})" : "";
        return $"{Name} x{Quantity}{category} - Modified: {LastModified:yyyy-MM-dd HH:mm}";
    }
}

public class GameContext : DbContext
{
    public DbSet<InventoryItem> Items { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseSqlite("Data Source=game.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>()
            .HasIndex(i => i.Name)
            .HasDatabaseName("IX_Items_Name");

        modelBuilder.Entity<InventoryItem>()
            .HasIndex(i => new { i.Name, i.PlayerId })
            .IsUnique()
            .HasDatabaseName("IX_Items_PlayerName_Unique");

        base.OnModelCreating(modelBuilder);
    }
}

public struct PlayerData
{
    public int Life { get; set; }       
    public string Name { get; set; }    
    public int posX { get; set; }      
    public int posY { get; set; }     
}

public record Checkpoint(string Name, int posX, int posY)
{

    public double DistanceTo(int x, int y)
    {
        return Math.Sqrt(Math.Pow(posX - x, 2) + Math.Pow(posY - y, 2));
    }

}

public class Player
{
    private PlayerData _data;

    public Player(int life = 50) => _data = new PlayerData 
    { 
        Life = life, 
        Name = "Player",  
        posX = 0,         
        posY = 0 
    };

    public int Life => _data.Life;
    public string Name => _data.Name;
    public int posX => _data.posX;
    public int posY => _data.posY;

    public void Hit(int value) => _data.Life -= value;

    public void RandomMove()
    {
        _data.posX = Random.Shared.Next(21); 
        _data.posY = Random.Shared.Next(21);
    }

    public void TpCheckpoint(Checkpoint checkpoint)
    {
        _data.posX = checkpoint.posX;
        _data.posY = checkpoint.posY;
    }

    public void TpToClosestCheckpoint(IEnumerable<Checkpoint> checkpoints)
    {
        if (!checkpoints.Any())
        {
            Console.WriteLine("Aucun checkpoint dispo !");
            return;
        }

        var closest = checkpoints.MinBy(cp => cp.DistanceTo(posX, posY));
        
        if (closest == null)
        {
            Console.WriteLine("Erreur : aucun checkpoint trouvé");
            return;
        }
        
        Console.WriteLine($"Checkpoint le plus proche: {closest.Name} " +
                         $"(distance: {closest.DistanceTo(posX, posY):F2})");
        
        TpCheckpoint(closest);
    }

    public void Save(string fileName = "StatSave.json")
    {
        try
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true 
            };
            
            string json = JsonSerializer.Serialize(_data, options);
            
            File.WriteAllText(fileName, json);
            
            Console.WriteLine($"Save réussie : {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la save : {ex.Message}");
        }
    }

    public void Load(string fileName = "StatSave.json")
    {
        try
        {
            if (!File.Exists(fileName))
            {
                Console.WriteLine($"Fichier {fileName} introuvable, création new player");
                _data = new PlayerData { Life = 50, Name = "Player", posX = 0, posY = 0 };
                return;
            }

            string json = File.ReadAllText(fileName);
            
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine("Fichier vide, création new player");
                _data = new PlayerData { Life = 50, Name = "Player", posX = 0, posY = 0 };
                return;
            }

            _data = JsonSerializer.Deserialize<PlayerData>(json);
            
            Console.WriteLine($"Chargement réussi depuis {fileName}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON corrompu : {ex.Message}");
            Console.WriteLine("Création new player...");
            _data = new PlayerData { Life = 50, Name = "Player", posX = 0, posY = 0 };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors du chargement : {ex.Message}");
            _data = new PlayerData { Life = 50, Name = "Player", posX = 0, posY = 0 };
        }
    }
}

public class InventoryManager
{
    private readonly GameContext _context;
    private readonly int _playerId;

    public InventoryManager(GameContext context, int playerId = 1)
    {
        _context = context;
        _playerId = playerId;
    }

    public void AddItem(string name, int quantity, string? category = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Nom d'item vide !");
            return;
        }

        if (quantity <= 0)
        {
            Console.WriteLine("Quantité doit être positive !");
            return;
        }

        try
        {
            var existingItem = _context.Items
                .FirstOrDefault(i => i.Name == name && i.PlayerId == _playerId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.LastModified = DateTime.Now;
                Console.WriteLine($"{name} : {existingItem.Quantity - quantity} + {quantity} = {existingItem.Quantity}");
            }
            else
            {
                var newItem = new InventoryItem
                {
                    Name = name,
                    Quantity = quantity,
                    Category = category,
                    PlayerId = _playerId,
                    LastModified = DateTime.Now
                };

                _context.Items.Add(newItem);
                Console.WriteLine($"Nouvel item ajouté : {name} x{quantity}");
            }

            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'ajout : {ex.Message}");
        }
    }

    public void ShowInventory()
    {
        try
        {
            var items = _context.Items
                .Where(i => i.PlayerId == _playerId)
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Name)
                .ToList();

            if (!items.Any())
            {
                Console.WriteLine("Inventaire vide");
                return;
            }

            Console.WriteLine($"Inventaire (Total: {items.Sum(i => i.Quantity)} items):");
            
            var groupedItems = items.GroupBy(i => i.Category ?? "Sans catégorie");
            
            foreach (var group in groupedItems)
            {
                Console.WriteLine($"\n{group.Key}:");
                foreach (var item in group)
                {
                    Console.WriteLine($"  - {item}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lecture inventaire : {ex.Message}");
        }
    }

    public void UpdateItem(string name, int newQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Nom d'item vide !");
            return;
        }

        if (newQuantity < 0)
        {
            Console.WriteLine("Quantité ne peut pas être négative !");
            return;
        }

        try
        {
            var item = _context.Items
                .FirstOrDefault(i => i.Name == name && i.PlayerId == _playerId);

            if (item == null)
            {
                Console.WriteLine($"Item '{name}' introuvable dans l'inventaire !");
                return;
            }

            int oldQuantity = item.Quantity;
            item.Quantity = newQuantity;
            item.LastModified = DateTime.Now;

            _context.SaveChanges();

            if (newQuantity == 0)
            {
                Console.WriteLine($"{name} : {oldQuantity} -> 0 (épuisé)");
            }
            else
            {
                Console.WriteLine($"{name} : {oldQuantity} -> {newQuantity}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la mise à jour : {ex.Message}");
        }
    }

    public void RemoveItem(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Nom d'item vide !");
            return;
        }

        try
        {
            var item = _context.Items
                .FirstOrDefault(i => i.Name == name && i.PlayerId == _playerId);

            if (item == null)
            {
                Console.WriteLine($"Item '{name}' introuvable !");
                return;
            }

            _context.Items.Remove(item);
            _context.SaveChanges();

            Console.WriteLine($"Item '{name}' supprimé (quantité: {item.Quantity})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la suppression : {ex.Message}");
        }
    }

    public void ShowStatsByCategory()
    {
        try
        {
            var stats = _context.Items
                .Where(i => i.PlayerId == _playerId)
                .GroupBy(i => i.Category ?? "Sans catégorie")
                .Select(g => new
                {
                    Category = g.Key,
                    ItemCount = g.Count(),
                    TotalQuantity = g.Sum(i => i.Quantity),
                    LastModified = g.Max(i => i.LastModified)
                })
                .OrderByDescending(s => s.TotalQuantity)
                .ToList();

            if (!stats.Any())
            {
                Console.WriteLine("Aucune statistique (inventaire vide)");
                return;
            }

            Console.WriteLine("Statistiques par catégorie :");
            foreach (var stat in stats)
            {
                Console.WriteLine($"{stat.Category} : {stat.ItemCount} types, {stat.TotalQuantity} total, dernière modif: {stat.LastModified:MM-dd HH:mm}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur statistiques : {ex.Message}");
        }
    }
}

public static class DatabaseSetup
{
    public static void EnsureDatabaseCreated()
    {
        using var context = new GameContext();
        
        try
        {
            context.Database.EnsureCreated();
            Console.WriteLine("Base de données SQLite prête (game.db)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur création base : {ex.Message}");
        }
    }

    public static void SeedTestData()
    {
        using var context = new GameContext();
        
        if (context.Items.Any())
        {
            Console.WriteLine("Données de test déjà présentes, pas de seed");
            return;
        }

        var testItems = new[]
        {
            new InventoryItem { Name = "Épée", Quantity = 1, Category = "Weapon", PlayerId = 1 },
            new InventoryItem { Name = "Potion", Quantity = 5, Category = "Item", PlayerId = 1 },
            new InventoryItem { Name = "Or", Quantity = 100, Category = "Currency", PlayerId = 1 },
            new InventoryItem { Name = "Bouclier", Quantity = 1, Category = "Weapon", PlayerId = 1 },
        };

        context.Items.AddRange(testItems);
        context.SaveChanges();
        
        Console.WriteLine($"{testItems.Length} items de test ajoutés");
    }
}


class Program
{
    static void Main()
    {
        Console.WriteLine("====================================================\n");

        Console.WriteLine("SETUP\n");
        DatabaseSetup.EnsureDatabaseCreated();
        DatabaseSetup.SeedTestData();
        Console.WriteLine();

        Console.WriteLine("EXERCICES 1-2\n");
        
        var player = new Player();
        var checkpoints = new List<Checkpoint>
        {
            new Checkpoint("Spawn", 0, 0),
            new Checkpoint("Forêt", 5, 3),
            new Checkpoint("Montagne", 15, 12),
            new Checkpoint("Village", 8, 6),
            new Checkpoint("Dungeon", 20, 20)
        };

        player.Load();
        Console.WriteLine($"Player: {player.Life} HP, Position ({player.posX}, {player.posY})");

        player.Hit(10);
        player.RandomMove();
        player.TpToClosestCheckpoint(checkpoints);
        player.Save();
        Console.WriteLine($"Après combat/mouvement: {player.Life} HP, Position ({player.posX}, {player.posY})");
        Console.WriteLine();

        Console.WriteLine("====================================================");
        Console.WriteLine("\nEXERCICE 3\n");

        using (var context = new GameContext())
        {
            var inventory = new InventoryManager(context, playerId: 1);
            
            Console.WriteLine("État initial de l'inventaire:");
            inventory.ShowInventory();
            
            inventory.AddItem("Arc", 1, "Weapon");         
            inventory.AddItem("Potion", 3, "Item");         
            inventory.AddItem("Gemme", 10, "Currency");      
            
            Console.WriteLine("\nInventaire après ajouts:");
            inventory.ShowInventory();

            Console.WriteLine("====================================================");
            Console.WriteLine("\nEXERCICE 4\n");
            
            inventory.UpdateItem("Potion", 12);           
            inventory.UpdateItem("Or", 50);                  
            
            inventory.RemoveItem("Bouclier");
            
            Console.WriteLine("\nTest contrainte unique:");
            inventory.AddItem("Arc", 2, "Weapon");        
            
            Console.WriteLine("\nInventaire final:");
            inventory.ShowInventory();

            Console.WriteLine("====================================================");
            Console.WriteLine("\nBONUS : Statistiques avancées\n");
            inventory.ShowStatsByCategory();


            Console.WriteLine("\n====================================================");
        }
    }
}