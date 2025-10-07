using System.Text.Json;

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

class Program
{
    static void Main()
    {
        Console.WriteLine("==============================================\n");
        Console.WriteLine("Init");

        var player = new Player();

        var checkpoints = new List<Checkpoint>
        {
            new Checkpoint("Spawn", 0, 0),
            new Checkpoint("Forêt", 5, 3),
            new Checkpoint("Montagne", 15, 12),
            new Checkpoint("Village", 8, 6),
            new Checkpoint("Dungeon", 20, 20)
        };

        Console.WriteLine($"Checkpoints créés :");
        foreach (var cp in checkpoints)
        {
            Console.WriteLine($"  - {cp.Name} : ({cp.posX}, {cp.posY})");
        }
        Console.WriteLine();

        Console.WriteLine("==============================================\n");

        player.Load();
        Console.WriteLine($"État initial après load :");
        Console.WriteLine($"Life : {player.Life} HP");
        Console.WriteLine($"Position : ({player.posX}, {player.posY})");
        Console.WriteLine();

        Console.WriteLine("Fight");
        player.Hit(10);
        Console.WriteLine($"  Après combat : {player.Life} HP (-10)");
        Console.WriteLine();

        Console.WriteLine("==============================================\n");

        player.RandomMove();
        Console.WriteLine($"Random move player : ({player.posX}, {player.posY})");

        Console.WriteLine("\nDistances vers chaque checkpoint :");
        foreach (var cp in checkpoints)
        {
            double distance = cp.DistanceTo(player.posX, player.posY);
            Console.WriteLine($"  - {cp.Name} : {distance:F2} unités");
        }
        Console.WriteLine();

        Console.WriteLine("Tp to closest Cp :");
        player.TpToClosestCheckpoint(checkpoints);
        Console.WriteLine($"Nouvelle position : ({player.posX}, {player.posY})");
        Console.WriteLine();


        Console.WriteLine("==============================================\n");

        Console.WriteLine("Final Save"); 
        player.Save();
    }
}