using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;

#region ENTITIYES

class User

{
public Guid Id { get; set; } = Guid.NewGuid();
public string Login { get; set; }
public string PasswordHash { get; set; }
public int Rating { get; set; }
public bool IsBot { get; set; }
}

class GameSession
{
    public Guid Id { get; set; }
    public Guid Player1Id { get; set; }
    public Guid Player2Id { get; set; }
    public Guid WinnerId { get; set; }
    
    public int RatingChange { get; set; }
    public DateTime PlayedAt { get; set; } = DateTime.Now;
    
    
}
#endregion
#region DAL

class UserRepository
{
    private List<User> users = new List<User>();
    public void Create(User user) => users.Add(user);

    public User GetById(Guid Id)
    {
        return users.FirstOrDefault(u => u.Id == Id);
    }
        public User GetByLogin(string login) => users.FirstOrDefault(x => x.Login == login);
        public List<User> GetAll() => users;
        public void Remove(Guid Id)
        {
            var u = GetById(Id);
            if (u != null) users.Remove(u);
        }
}

class SessionRepository
{
    private List<GameSession> sessions = new List<GameSession>();
    public void Create(GameSession session) => sessions.Add(session);
    public GameSession GetById(Guid Id) => 
        sessions.FirstOrDefault(x => x.Id == Id);
    public List<GameSession> GetAll() => sessions;

    public void Remove(Guid Id)
    {
        var s = GetById(Id);
        if (s != null)
            sessions.Remove(s);
    }
}
#endregion

static class PasswordHasher
{
    public static string Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}

#region AL

class GameService
{
    private UserRepository userRepo;
    private SessionRepository sessionRepo;
    private Random random = new Random();
    public User CurrentUser { get; private set; }
    public User Bot { get; private set; }

    public GameService(UserRepository u, SessionRepository s)
    {
        userRepo = u;
        sessionRepo = s;
        Bot = new User
        {
            Login = "BOT",
            PasswordHash = "",
            IsBot = true,
        };
        userRepo.Create(Bot);

    }

    public bool Register(string login, string password)
    {
        if (userRepo.GetByLogin(login) != null)
            return false;
        userRepo.Create(new User
            {
            Login=login,
         PasswordHash = PasswordHasher.Hash(password)       
    });
    return true;
}

public bool Login(string login, string password)
{
    var u = userRepo.GetByLogin(login);
    if (u == null) return false;
    if (u.PasswordHash == PasswordHasher.Hash(password))
    {
        CurrentUser = u;
        return true;
    }

    return false;
}
public void Logout() => CurrentUser = null;

public void DeleteAccount()
{
    if (CurrentUser == null) return;

    userRepo.Remove(CurrentUser.Id);
    CurrentUser = null;
}

public GameSession PlayGame()
{
    if (CurrentUser == null) return null;

    int pRoll = random.Next(1, 7);
    int bRoll = random.Next(1, 7);

    Console.WriteLine($"Ти кинув: {pRoll}");
    Console.WriteLine($"Бот кинув: {bRoll}");

    var session = new GameSession
    {
        Player1Id = CurrentUser.Id,
        Player2Id = Bot.Id,
        RatingChange = 25
    };
    
        if (pRoll > bRoll)
        {
            session.WinnerId = CurrentUser.Id;
            CurrentUser.Rating += 25;
            Console.WriteLine("Ти попустив бота, ма бой!");
        }

        if (pRoll == bRoll)
        {
            Console.WriteLine("Ви обидва антігулі, zxc CHMOSHNIKI.");
            return null;
        }
        else
        {
            session.WinnerId = Bot.Id;
            CurrentUser.Rating -= 25;
            Console.WriteLine("Бож, чєєєєєєл, ти лузнув боту, умойся слєзамі і тд.");
        }

        sessionRepo.Create(session);
        Console.WriteLine($"Твій рейтинг: {CurrentUser.Rating}");
        return session;
    }

    public List<GameSession> MySession()
    {
        if (CurrentUser == null) return new List<GameSession>();

        return sessionRepo.GetAll()
            .Where(s => s.Player1Id == CurrentUser.Id)
            .ToList();
    }

    public void DeleteSession(Guid id)
    {
        sessionRepo.Remove(id);
    }
}
#endregion

class Program
{
    static void Main()
    {
        var userRepo = new UserRepository();
        var sessionRepo = new SessionRepository();
        var game = new GameService(userRepo, sessionRepo);
        while (true)
        {
            Console.WriteLine("\n1.Register 2.Login 3.Play 4.MyGames 5.Delete Acc 6.Logout 0.Exit");
            var cmd = Console.ReadLine();

            switch (cmd)
            {
                case "1":
                    Console.Write("Login: ");
                    var rLogin = Console.ReadLine();
                    Console.Write("Password: ");
                    var rPass= Console.ReadLine();

                    Console.WriteLine(
                        game.Register(rLogin, rPass)
                            ? "OK"
                            : "Login exists");
                        break;
                case "2":
                    Console.Write("Login: ");
                    var l = Console.ReadLine();
                    Console.Write("Password: ");
                    var p = Console.ReadLine();
                    Console.WriteLine(
                        game.Login(l, p)
                    ? "Logged in"
                    : "Fail");
                    break;
                case "3":
                    game.PlayGame();
                    break;
                case "4":
                    foreach (var s in game.MySession())
                        Console.WriteLine($"{s.Id} | {s.PlayedAt}");
                    break;
                case "5":
                    game.DeleteAccount();
                    Console.WriteLine("Deleted");
                    break;
                case "6":
                    game.Logout();
                    break;
                case "0":
                    return;
                    
            }
        }
    }
}