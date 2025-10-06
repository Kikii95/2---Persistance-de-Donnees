using System.Text.Json;
using System.Xml.Linq;

public struct PlayerData
{
    public int Life { get; set; }
    public string Name { get; set; }
    public int posX { get; set; }
    public int posY { get; set; }
}

public class Checkpoint
{
    public string Name { get; private set; }
    public int posX { get; private set; } = 0;
    public int posY { get; private set; } = 0;

    public Checkpoint(string name, int X, int Y)
    {
        Name = name;
        posX = X;
        posY = Y;
    }
}

public class Player
{
    private PlayerData _data;

    public Player(int life = 50) => _data = new PlayerData { Life = life, Name = "Player", posX = 0, posY = 0 };

    public int Life => _data.Life;
    public string Name => _data.Name;
    public int posX => _data.posX;
    public int posY => _data.posY;

    public void Hit(int value) => _data.Life -= value;

    public void RandomMove()
    {
        _data.posX = Random.Shared.Next(10);
        _data.posY = Random.Shared.Next(10);
    }

    public void TpCheckpoint(Checkpoint checkpoint)
    {
        _data.posX = checkpoint.posX;
        _data.posY = checkpoint.posY;
    }

    public void Save(string fileName = "StatSave.json")
    {

        string json = JsonSerializer.Serialize(_data);
        File.WriteAllText(fileName, json);
    }

    public void Load(string fileName = "StatSave.json")
    {
        if (!File.Exists(fileName))
        {
            _data = new PlayerData { Life = 50, Name = "Player", posX = 0, posY = 0 };
            return;
        }

        string json = File.ReadAllText(fileName);

        _data = JsonSerializer.Deserialize<PlayerData>(json);
    }
}

class Program
{
    static void Main()
    {
        var player = new Player();
        var checkpoint = new Checkpoint("Joe", Random.Shared.Next(20), Random.Shared.Next(20));

        player.Load();
        Console.WriteLine($"Life initiale: {player.Life}");
        Console.WriteLine($"Position initiale: {player.posX}, {player.posY}");
        Console.WriteLine($"Checkpoint: {checkpoint.posX}, {checkpoint.posY}");
        Console.WriteLine("-----------------------------------------------------------------");

        player.Hit(5);
        Console.WriteLine($"Après hit: {player.Life}");
        Console.WriteLine("-----------------------------------------------------------------");

        player.RandomMove();
        Console.WriteLine($"Random position: {player.posX}, {player.posY}");
        Console.WriteLine("-----------------------------------------------------------------");

        player.TpCheckpoint(checkpoint);
        Console.WriteLine($"Tp to checkpoint: {player.posX}, {player.posY}");
        Console.WriteLine("-----------------------------------------------------------------");

        player.Save();
        Console.WriteLine("Sauvegarde effectuée (StatSave.json).");
    }
}