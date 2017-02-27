using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class Player
{
    static Map _map = new Map();

    static void Main(string[] args)
    {
        int factoryCount = int.Parse(Console.ReadLine()); // the number of factories
        int linkCount = int.Parse(Console.ReadLine()); // the number of links between factories
        for (int i = 0; i < linkCount; i++)
        {
            var inputs = Console.ReadLine().Split(' ');
            _map.AddLink(int.Parse(inputs[0]), int.Parse(inputs[1]), int.Parse(inputs[2]));
        }

        _map.Initialize();

        GameLoop();
    }

    private static string[] GameLoop()
    {
        string[] inputs;
        string entityType;
        int entityCount;
        int entityId;
        int arg1;
        int arg2;
        int arg3;
        int arg4;
        int arg5;
        List<Troop> troops = new List<Troop>();
        List<Bomb> bombs = new List<Bomb>();

        while (true)
        {
            troops.Clear();
            bombs.Clear();

            entityCount = int.Parse(Console.ReadLine()); // the number of entities (e.g. factories and troops)
            for (int i = 0; i < entityCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                entityType = inputs[1];
                entityId = int.Parse(inputs[0]);
                arg1 = int.Parse(inputs[2]);
                arg2 = int.Parse(inputs[3]);
                arg3 = int.Parse(inputs[4]);
                arg4 = int.Parse(inputs[5]);
                arg5 = int.Parse(inputs[6]);

                switch (inputs[1])
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
    public const string WAIT = "WAIT";
    public const string MOVE = "MOVE";
    public const string Bomb = "BOMB";
}

public class Factory
{
    public int Id;
    public int Owner;
    public int production;
    public int nbCyborgs;
    public bool needHelp;

    public Dictionary<int, int> steps = new Dictionary<int, int>();
    public List<Tuple<int, int>> OrderedSteps = new List<Tuple<int, int>>();
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
    Dictionary<int, Factory> _factories = new Dictionary<int, Factory>();
    Dictionary<int, string> _actions = new Dictionary<int, string>();
    Dictionary<int, List<int>> _ennemyBombs = new Dictionary<int, List<int>>();
    int _nbBomb = 2;

    public void AddLink(int id1, int id2, int step)
    {
        if (_factories.ContainsKey(id1) == false)
        {
            _factories.Add(id1, new Factory { Id = id1 });
        }

        _factories[id1].steps[id2] = step;

        if (_factories.ContainsKey(id2) == false)
        {
            _factories.Add(id2, new Factory { Id = id2 });
        }

        _factories[id2].steps[id1] = step;
    }

    internal void Initialize()
    {
        foreach (var factory in _factories.Values)
        {
            foreach (var o in factory.steps.OrderBy(f => f.Value))
            {
                factory.OrderedSteps.Add(new Tuple<int, int>(o.Key, o.Value));
            }
        }
    }

    public void UpdateFactory(int factoryId, int owner, int nbCyborgs, int productionNb)
    {
        _factories[factoryId].Owner = owner;
        _factories[factoryId].nbCyborgs = nbCyborgs;
        _factories[factoryId].production = productionNb;
        _factories[factoryId].needHelp = false;
    }

    internal string DoAction(List<Troop> troops, List<Bomb> bombs)
    {
        const int prodWeight = 15;
        const int stepWeight = 5;
        const int nbCyborgWeight = 1;

        string action = string.Empty;
        _actions.Clear();
        var sources = _factories
            .Where(kvp => kvp.Value.Owner == Owner.Player)
            .OrderByDescending(f => f.Value.nbCyborgs)
            .ToList();

        var factoryBombWarning = _ennemyBombs.SelectMany(x => x.Value).ToList();

        //bombs
        foreach (var bomb in bombs)
        {
            if (_ennemyBombs.ContainsKey(bomb.Id) == false)
            {
                _ennemyBombs.Add(bomb.Id, sources.Select(x => x.Key).ToList());
            }
        }

        var others = _factories.Where(kvp => kvp.Value.Owner != Owner.Player && kvp.Value.production > 0).ToList();
        var othersByWeight = others.ToDictionary(x => x.Value.Id, x => 0);

        //compute weight
        foreach (var source in sources)
        {
            var myOthers = others.Select(kvp => new
               {
                   Id = kvp.Value.Id,
                   nbCyborgs = kvp.Value.nbCyborgs,
                   Owner = kvp.Value.Owner,
                   production = kvp.Value.production,
                   //nbCyborgsAfterAttack = kvp.Value.nbCyborgs +
                   // troops
                   //     .Where(t => t.Owner == Owner.Enemy && t.FactoryEnd == kvp.Value.Id)
                   //     .Sum(t => t.NbCyborgs) -
                   // troops
                   //     .Where(t => t.Owner == Owner.Player && t.FactoryEnd == kvp.Value.Id)
                   //     .Sum(t => t.NbCyborgs),
                   weight = kvp.Value.production * prodWeight - kvp.Value.steps[source.Value.Id] * stepWeight - kvp.Value.nbCyborgs * nbCyborgWeight
               });

            foreach (var other in myOthers)
            {
                othersByWeight[other.Id] += other.weight;
            }
         }

        var enemies = othersByWeight
            .OrderByDescending(x => x.Value)
            .ToList();

        //Help ?
        /*foreach (var source in sources.Where(s => s.Value.production > 1 && !factoryBombWarning.Contains(s.Value.Id)))
        {
            var enemiesAttack = troops
               .Where(t => t.Owner == Owner.Enemy && t.FactoryEnd == source.Value.Id)
               .ToList();

            var attack = enemiesAttack.Sum(t => t.NbCyborgs) + bombs.Where(b => b.Owner == Owner.Enemy && b.FactoryEnd == source.Value.Id).Sum(x => 10);

            var friends = troops
               .Where(t => t.Owner == Owner.Player && t.FactoryEnd == source.Value.Id)
               .Sum(t => t.NbCyborgs);

            if (attack - friends > source.Value.nbCyborgs)
            {
                _actions[source.Value.Id] = null;

                //compute with production
                var enemiesAttackMinSteps = enemiesAttack.Count > 0 ? enemiesAttack.Min(t => t.Steps) : 1;
                var nbMyCyborg = source.Value.production * enemiesAttackMinSteps + source.Value.nbCyborgs;

                if (attack - friends > nbMyCyborg)
                {
                    source.Value.needHelp = true;

                    var savers = sources
                        .Where(x => x.Value.Id != source.Value.Id && x.Value.needHelp == false && _actions.ContainsKey(x.Key) == false)
                        .OrderBy(x => source.Value.steps[x.Value.Id]);

                    foreach (var saver in savers)
                    {
                        var totalCyborgs = attack - friends - nbMyCyborg;

                        if (totalCyborgs > 0)
                        {
                            friends += saver.Value.nbCyborgs;
                            _actions[saver.Value.Id] = Move(saver.Value.Id, source.Value.Id, totalCyborgs);

                            saver.Value.nbCyborgs -= totalCyborgs;
                            troops.Add(new Troop
                            {
                                FactoryStart = saver.Value.Id,
                                FactoryEnd = source.Value.Id,
                                NbCyborgs = totalCyborgs,
                                Owner = Owner.Player,
                                Steps = source.Value.steps[saver.Value.Id]
                            });
                        }
                        else
                        {
                            break;
                        }                        
                    }
                }

                continue;
            }
        }*/

        //attack
        while (enemies.Count > 0)
        {
            foreach (var source in sources)
            {
                if (enemies.Count == 0) break;
                if (source.Value.nbCyborgs <= 3) continue;

                var other = _factories[enemies[0].Key];

                var enemyTroop = troops
                    .Where(t => t.Owner == Owner.Enemy && t.FactoryEnd == other.Id && t.Steps <= source.Value.steps[other.Id])
                    .Sum(t => t.NbCyborgs) + bombs.Where(b => b.Owner == Owner.Enemy && b.FactoryEnd == source.Value.Id).Sum(x => 10);
                var production = (other.Owner == Owner.Enemy ? other.production * (source.Value.steps[other.Id] + 1) : 0);
                var otherNbCyborgs = other.nbCyborgs + production + enemyTroop;

                var playerTroop = troops
                    .Where(t => t.Owner == Owner.Player && t.FactoryEnd == other.Id)
                    .Sum(t => t.NbCyborgs);

                if (playerTroop - otherNbCyborgs > 0) // attack will conquer the factory
                {                    
                    break;
                }

                var totalCyborgs = source.Value.nbCyborgs; //otherNbCyborgs - playerTroop + 1;
                _actions[source.Value.Id] = Move(source.Value.Id, other.Id, totalCyborgs);
                source.Value.nbCyborgs -= totalCyborgs;
                troops.Add(new Troop
                {
                    FactoryStart = source.Value.Id,
                    FactoryEnd = other.Id,
                    NbCyborgs = otherNbCyborgs,
                    Owner = Owner.Player,
                    Steps = source.Value.steps[other.Id]
                });
            }

            enemies.RemoveAt(0);
        }

        TryToLaunchBomb(troops, bombs);

        List<string> actions = new List<string>();
        foreach (var a in _actions)
        {
            if (a.Value != null)
            {
                actions.Add(a.Value);
            }
        }

        if (actions.Count > 0)
        {
            return string.Join(";", actions);
        }

        return Wait();
    }

    private void TryToLaunchBomb(List<Troop> troops, List<Bomb> bombs)
    {
        const int minCyborg = 0; // 10

        if (_nbBomb == 0) return;

        var enemies = _factories
            .Where(kvp => kvp.Value.Owner == Owner.Enemy
                && kvp.Value.production == 3
                && kvp.Value.nbCyborgs >= minCyborg).ToList();

        foreach (var enemy in enemies)
        {
            if (_nbBomb == 0) break;

            var id = enemy.Value.Id;

            if (bombs
                .Where(b => b.Owner == Owner.Player && b.FactoryEnd == id)
                .Any())
            {
                continue;
            }

            var nearests = enemy.Value.steps
                .Where(kvp => _factories[kvp.Key].Owner == Owner.Player)
                .OrderBy(kvp => _factories[id].steps[kvp.Key])
                .ToList();

            if (nearests.Count > 0)
            {
                var nearest = nearests.First();
                _actions[id] = Bomb(nearest.Key, id);
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

    string Move(int src, int dst, int nb)
    {
        nb = nb > 0 ? nb : 1;
        return $"{Action.MOVE} {src} {dst} {nb}";
    }

    string Bomb(int src, int dst)
    {
        return $"{Action.Bomb} {src} {dst}";
    }

    string Wait()
    {
        return $"{Action.WAIT}";
    }
}