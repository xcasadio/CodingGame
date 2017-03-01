using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class Player
{
    static Map _map = new Map();

    static void Main(string[] args)
    {
        Console.ReadLine(); // the number of factories
        int linkCount = int.Parse(Console.ReadLine()); // the number of links between factories
        for (int i = 0; i < linkCount; i++)
        {
            var inputs = Console.ReadLine().Split(' ');
            _map.AddLink(int.Parse(inputs[0]), int.Parse(inputs[1]), int.Parse(inputs[2]));
        }

        _map.Initialize();

        GameLoop();
    }

    private static void GameLoop()
    {
        var troops = new List<Troop>();
        var bombs = new List<Bomb>();

        while (true)
        {
            troops.Clear();
            bombs.Clear();

            var entityCount = int.Parse(Console.ReadLine());
            for (int i = 0; i < entityCount; i++)
            {
                var inputs = Console.ReadLine().Split(' ');
                var entityType = inputs[1];
                var entityId = int.Parse(inputs[0]);
                var arg1 = int.Parse(inputs[2]);
                var arg2 = int.Parse(inputs[3]);
                var arg3 = int.Parse(inputs[4]);
                var arg4 = int.Parse(inputs[5]);
                var arg5 = int.Parse(inputs[6]);

                switch (entityType)
                {
                    case EntityType.Factory:
                        _map.UpdateFactory(entityId, arg1, arg2, arg3);
                        break;

                    case EntityType.Troop:
                        troops.Add(new Troop
                        {
                            Id = entityId,
                            FactoryStart = arg2,
                            FactoryEnd = arg3,
                            Owner = arg1,
                            NbCyborgs = arg4,
                            Steps = arg5
                        });
                        break;

                    case EntityType.Bomb:
                        bombs.Add(new Bomb
                        {
                            Id = entityId,
                            FactoryStart = arg2,
                            FactoryEnd = arg3,
                            Owner = arg1,
                            Steps = arg4
                        });
                        break;
                }
            }

            Console.WriteLine(_map.DoAction(troops, bombs));
        }
    }
}

public class Owner
{
    public const int Player = 1;
    public const int Neutral = 0;
    public const int Enemy = -1;
}

public class EntityType
{
    public const string Factory = "FACTORY";
    public const string Troop = "TROOP";
    public const string Bomb = "BOMB";
}

public class Action
{
    public const string Wait = "WAIT";
    public const string Move = "MOVE";
    public const string Bomb = "BOMB";
    public const string Increase = "INC";
}

public class Factory
{
    public int Id;
    public int Owner;
    public int Production;
    public int NbCyborgs;

    public Dictionary<int, int> Steps = new Dictionary<int, int>();
    public List<Tuple<int, int>> OrderedSteps = new List<Tuple<int, int>>();

    public bool NeedHelp;
    public int Weight;
}

public class Troop
{
    public int Id;
    public int Owner;
    public int FactoryStart;
    public int FactoryEnd;
    public int NbCyborgs;
    public int Steps;
}

public class Bomb
{
    public int Id;
    public int Owner;
    public int FactoryStart;
    public int FactoryEnd;
    public int Steps;
}


public class Map
{
    readonly Dictionary<int, Factory> _factories = new Dictionary<int, Factory>();
    readonly Dictionary<int, List<string>> _actions = new Dictionary<int, List<string>>();
    //readonly Dictionary<int, List<int>> _ennemyBombs = new Dictionary<int, List<int>>();
    int _nbBomb = 2;

    public void AddLink(int id1, int id2, int step)
    {
        if (_factories.ContainsKey(id1) == false)
        {
            _factories.Add(id1, new Factory { Id = id1 });
        }

        _factories[id1].Steps[id2] = step;

        if (_factories.ContainsKey(id2) == false)
        {
            _factories.Add(id2, new Factory { Id = id2 });
        }

        _factories[id2].Steps[id1] = step;
    }

    internal void Initialize()
    {
        foreach (var factory in _factories.Values)
        {
            foreach (var o in factory.Steps.OrderBy(f => f.Value))
            {
                factory.OrderedSteps.Add(new Tuple<int, int>(o.Key, o.Value));
            }
        }
    }

    public void UpdateFactory(int factoryId, int owner, int nbCyborgs, int productionNb)
    {
        _factories[factoryId].Owner = owner;
        _factories[factoryId].NbCyborgs = nbCyborgs;
        _factories[factoryId].Production = productionNb;
        _factories[factoryId].NeedHelp = false;
        _factories[factoryId].Weight = 0;
    }

    internal string DoAction(List<Troop> troops, List<Bomb> bombs)
    {
        _actions.Clear();
        var myFactories = _factories
            .Where(kvp => kvp.Value.Owner == Owner.Player)
            .ToList();

        var enemies = _factories
            .Where(f => f.Value.Owner != Owner.Player && f.Value.Production > 0)
            .ToList();

        foreach (var source in myFactories)
        {
            if (source.Value.NbCyborgs == 0) continue;

            if (FleeOrWait(source.Value, troops)) continue;

            TryIncrease(source.Value);

            var nearestEnemies = enemies
                .OrderBy(f => source.Value.Steps[f.Value.Id])
                .ThenBy(f => f.Value.NbCyborgs)
                .ThenByDescending(f => f.Value.Owner)
                .ToList();

            var nbAttack = 0;

            foreach (var enemy in nearestEnemies)
            {
                if (source.Value.NbCyborgs == 0 || nbAttack == 2) break;

                if (Attack(source.Value, enemy.Value, troops, bombs))
                {
                    nbAttack++;
                }
            }
        }

        TryToLaunchBomb(bombs);

        var actions = new List<string>();
        foreach (var list in _actions)
        {
            actions.AddRange(list.Value);
        }

        if (actions.Count > 0)
        {
            return string.Join(";", actions);
        }

        return Wait();
    }

    private void TryIncrease(Factory factory)
    {
        if (factory.NbCyborgs >= 10 && factory.Production < 3)
        {
            if (_actions.ContainsKey(factory.Id) == false) _actions[factory.Id] = new List<string>();
            _actions[factory.Id].Add(Increase(factory.Id));
            factory.NbCyborgs -= 10;
            factory.Production += 1;
        }
    }

    private bool FleeOrWait(Factory factory, List<Troop> troops)
    {
        var enemiesTroops = troops
            .Where(t =>
                    t.Owner == Owner.Enemy && t.FactoryEnd == factory.Id)
            .Sum(t => t.NbCyborgs);

        var playerTroops = troops
            .Where(t => t.Owner == Owner.Player && t.FactoryEnd == factory.Id)
            .Sum(t => t.NbCyborgs);

        if (enemiesTroops < factory.NbCyborgs + playerTroops)
        {
            return false;
        }

        //var nearestPlayers = _factories
        //    .Select(t => t.Value)
        //    .Where(t => t.Owner == Owner.Player && t.Id != factory.Id)
        //    .OrderBy(t => t.Steps[factory.Id])
        //    .FirstOrDefault();
        //
        //if (nearestPlayers != null)
        //{
        //    if (_actions.ContainsKey(factory.Id) == false) _actions[factory.Id] = new List<string>();
        //    _actions[factory.Id].Add(Move(factory.Id, nearestPlayers.Id, factory.NbCyborgs));
        //
        //    troops.Add(new Troop
        //    {
        //        FactoryStart = factory.Id,
        //        FactoryEnd = nearestPlayers.Id,
        //        NbCyborgs = factory.NbCyborgs,
        //        Owner = Owner.Player,
        //        Steps = factory.Steps[nearestPlayers.Id]
        //    });
        //    factory.NbCyborgs = 0;
        //
        //    return true;
        //}

        return true;
    }

    private bool Attack(Factory source, Factory enemy, List<Troop> troops, List<Bomb> bombs, bool force = false)
    {
        var enemiesTroops = troops
                    .Where(t =>
                            t.Owner == Owner.Enemy && t.FactoryEnd == enemy.Id &&
                            t.Steps <= source.Steps[enemy.Id]).ToList();
        var nbEnemyTroops = enemiesTroops.Sum(t => t.NbCyborgs);
        var production = enemy.Owner == Owner.Enemy ? enemy.Production * (source.Steps[enemy.Id] + 1) : 0;
        var factor = enemy.Owner == Owner.Enemy ? 1 : -1;
        var otherNbCyborgs = enemy.NbCyborgs + production + nbEnemyTroops * factor;

        var enemyId = enemy.Id;

        //var playerTroops = troops
        //    .Where(t => t.Owner == Owner.Player && t.FactoryEnd == enemy.Id)
        //    .ToList();
        //var nbPlayerTroops = playerTroops.Sum(t => t.NbCyborgs);
        //
        //if (force == false && (nbPlayerTroops - otherNbCyborgs > 0)) // attack will conquer the factory
        //{
        //    return;
        //}
        //
        //var enemyId = enemy.Id;
        ////Can find a owned factory near the enemy?
        var nearestFriend = _factories
            .Where(f => f.Value.Owner != Owner.Enemy && f.Value.Id != source.Id && _factories[enemyId].Steps.ContainsKey(enemyId))
            .OrderBy(f => f.Value.Steps[enemyId]).ToList();

        if (nearestFriend.Count > 0)
        {
            var friendFactory = nearestFriend.First().Value;
            if (friendFactory.Steps[enemyId] < source.Steps[enemyId])
            {
                enemyId = friendFactory.Id;
            }
        }

        //
        var totalCyborgs = otherNbCyborgs /*- nbPlayerTroops*/ + 1;
        totalCyborgs = source.NbCyborgs - totalCyborgs < 0 ? source.NbCyborgs : totalCyborgs;

        if (source.NbCyborgs < 2)
        {
            return false;
        }

        if (_actions.ContainsKey(source.Id) == false) _actions[source.Id] = new List<string>();
        _actions[source.Id].Add(Move(source.Id, enemyId, totalCyborgs));
        source.NbCyborgs -= totalCyborgs;
        troops.Add(new Troop
        {
            FactoryStart = source.Id,
            FactoryEnd = enemyId,
            NbCyborgs = totalCyborgs,
            Owner = Owner.Player,
            Steps = source.Steps[enemyId]
        });

        return true;
    }

    private void TryToLaunchBomb(List<Bomb> bombs)
    {
        const int minCyborg = 0;

        if (_nbBomb == 0) return;

        var enemies = _factories
            .Where(kvp => kvp.Value.Owner == Owner.Enemy
                && kvp.Value.Production > 1
                && kvp.Value.NbCyborgs >= minCyborg).ToList();

        foreach (var enemy in enemies)
        {
            if (_nbBomb == 0) break;

            var id = enemy.Value.Id;

            if (bombs.Any(b => b.Owner == Owner.Player && b.FactoryEnd == id))
            {
                continue;
            }

            var nearests = enemy.Value.Steps
                .Where(kvp => _factories[kvp.Key].Owner == Owner.Player)
                .OrderBy(kvp => _factories[id].Steps[kvp.Key])
                .ToList();

            if (nearests.Count > 0)
            {
                var nearest = nearests.First();

                if (_actions.ContainsKey(id) == false) _actions[id] = new List<string>();
                _actions[id].Add(Bomb(nearest.Key, id));

                bombs.Add(new Bomb
                {
                    Owner = Owner.Player,
                    FactoryStart = nearest.Key,
                    FactoryEnd = id,
                    Id = 0,
                    Steps = 0
                });

                _nbBomb--;
            }
        }
    }

    static string Move(int src, int dst, int nb)
    {
        nb = nb > 0 ? nb : 1;
        return $"{Action.Move} {src} {dst} {nb}";
    }

    static string Bomb(int src, int dst)
    {
        return $"{Action.Bomb} {src} {dst}";
    }

    static string Increase(int id)
    {
        return $"{Action.Increase} {id}";
    }

    static string Wait()
    {
        return $"{Action.Wait}";
    }
}