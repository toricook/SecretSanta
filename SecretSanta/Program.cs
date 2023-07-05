using SecretSanta;
using System.Configuration;


/// <summary>
/// A program for assigning each person in the family another person to give a gift to. Takes in a list of players 
/// and emails, and emails each person with their secret santa. People will not be assigned to get a gift for their
/// own spouse/significant other
/// </summary>
await Run();


static async Task Run()
{
    string file = ConfigurationManager.AppSettings["PlayerFile"];
    if (!File.Exists(file))
    {
        throw new FileNotFoundException();
    }
    Player[] players = ReadFile(file);

    while (true)
    {
        try
        {
            AssignSecretSantas(players);
            break;
        }
        catch
        {
            Console.WriteLine("Assignment failed. Trying again");
        }
    }
    if (QualityCheck(players))
    {
        await SendEmails(players);
    }

}

static async Task SendEmails(Player[] players)
{
    var credentials = await EmailUtils.GetGmailCredentials(ConfigurationManager.AppSettings["ClientId"],
        ConfigurationManager.AppSettings["ClientSecret"]);
    Console.WriteLine("Gmail authentication successful");
    foreach (var p in players)
    {
        EmailUtils.SendGmail(ConfigurationManager.AppSettings["UserGmail"], credentials.Token.AccessToken, p.Email,
            "Your Secret Santa",
            $"Hello, {p.Name}," + Environment.NewLine + Environment.NewLine +
            $"Your secret santa is {p.SecretSantee.Name}. Thank you for using SantaBot5000. Merry Christmas!");
    }
}

/// <summary>
/// Read in a headerless CSV with Name,Email,Spouse for each player (spouse can be omitted)
/// </summary>
static Player[] ReadFile(string file)
{
    string[] data = File.ReadAllLines(file);
    return data.Select(i => Player.FromCsv(i)).ToArray();
}

static void AssignSecretSantas(Player[] players)
{
    Random r = new Random();
    List<Player> assigned = new List<Player>();
    foreach (Player p in players)
    {
        int attempts = 0;

        while (true)
        {
            // Select a random player as a candidate
            Player candidate = players[r.Next(0, players.Length)];

            // If no one else has been given this candidate, and the candidate is not this player, and 
            // the candidate is not the spouse of this player, then the candidate is valid
            if (!assigned.Contains(candidate) && candidate != p && !p.IsSpouse(candidate))
            {
                p.SecretSantee = candidate;
                assigned.Add(candidate);
                break;
            }
            attempts++;
            if (attempts == players.Length) // If we have tried every player
            {
                throw new Exception($"Could not find a valid secret santee for {p.Name}");
            }
        }
    }
}

/// <summary>
/// Test utility to print results to console for examination
/// </summary>
static void PrintSecretSantas(Player[] players)
{
    foreach (var p in players)
    {
        Console.WriteLine($"{p.Name} -> {p.SecretSantee.Name}");
    }
}

/// <summary>
/// Check if result is valid
/// </summary>
static bool QualityCheck(Player[] players)
{
    bool passed = true;
    IEnumerable<Player> secretSantas = players.Select(i => i.SecretSantee);
    if (secretSantas.Any(i => i is null))
    {
        Console.WriteLine("Not everyone got a secret santa");
        passed = false;
    }
    if (!secretSantas.All(players.Contains))
    {
        Console.WriteLine("List of secret santas not equal to list of players");
        passed = false;
    }
    if (secretSantas.Any(i => i.SecretSantee == i))
    {
        Console.WriteLine("Someone got themselves");
        passed = false;
    }
    if (secretSantas.Any(i => i.SecretSantee.Name == i.SpouseName))
    {
        Console.WriteLine("Someone got their spouse");
        passed = false;
    }
    if (passed)
    {
        Console.WriteLine("All tests passed");
    }
    return passed;
}

internal class Player
{
    public readonly string Name;
    public readonly string Email;
    public readonly string SpouseName;

    public Player(string name, string email, string spouseName)
    {
        Name = name;
        Email = email;
        SpouseName = spouseName;
    }

    // The player to whom this player will give a gift
    public Player SecretSantee { get; set; }

    public bool IsSpouse(Player player)
    {
        return player.Name == SpouseName;
    }

    public static Player FromCsv(string csv)
    {
        var s = csv.Split(',');
        return new Player(s[0], s.Length > 1 ? s[1] : null, s.Length > 2 ? s[2] : null);
    }
}
