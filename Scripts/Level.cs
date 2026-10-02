using Godot;
using System.Collections.Generic;

public enum Tile
{
	Empty = 0,
	Solid = 1,
	Plat = 2,
	Gate = 3,
	Hazard = 4
}

public static class G
{
	public const int T = 32;
	public const int WW = 208;
	public const int WH = 72;

	public static readonly Color Ink = new Color("#dbe7ff");
	public static readonly Color Dim = new Color("#7c8db0");
	public static readonly Color Blue = new Color("#38b6ff");
	public static readonly Color Red = new Color("#ff4d5e");
	public static readonly Color Green = new Color("#7dff8a");
	public static readonly Color Gold = new Color("#ffd166");
	public static readonly Color Ooze = new Color("#8bff4d");

	public static Vector2i Tiles(Vector2 p) => new Vector2i((int)Mathf.Floor(p.X / T), (int)Mathf.Floor(p.Y / T));
}

public class Room
{
	public string Name;
	public int X, Y, W, H, Zone;
	public bool Seen;

	public Room(string n, int x, int y, int w, int h, int z)
	{
		Name = n; X = x; Y = y; W = w; H = h; Zone = z;
	}

	public int X2 => X + W - 1;
	public int Y2 => Y + H - 1;
	public Rect2 World => new Rect2(X * G.T, Y * G.T, W * G.T, H * G.T);
	public int FloorRow => Y2 + 1;

	public bool Contains(Vector2 p)
	{
		return p.X >= X * G.T && p.X <= (X2 + 1) * G.T && p.Y >= Y * G.T && p.Y <= (Y2 + 1) * G.T;
	}
}

public class Link
{
	public int Ax, Ay, Bx, By;
	public string Req;
	public bool VertFirst;
}

public class Gate
{
	public List<Vector2i> Tiles = new List<Vector2i>();
	public string Req = "";
	public bool Open;
	public Vector2 Pos;
}

public class PlatDef
{
	public int X, Y, W;
	public PlatDef(int x, int y, int w) { X = x; Y = y; W = w; }
}

public class HazardDef
{
	public int X, Y, W, H;
	public HazardDef(int x, int y, int w, int h) { X = x; Y = y; W = w; H = h; }
}

public class ItemDef
{
	public string Kind;
	public int Tx, Ty;
	public float Amount = 1;
	public ItemDef(string k, int tx, int ty, float a = 1) { Kind = k; Tx = tx; Ty = ty; Amount = a; }
}

public class StationDef
{
	public string Kind;
	public int Tx, Ty;
	public StationDef(string k, int tx, int ty) { Kind = k; Tx = tx; Ty = ty; }
}

public class SpawnDef
{
	public string Type;
	public int Room, Count;
	public SpawnDef(string t, int room, int count) { Type = t; Room = room; Count = count; }
}

public static class Level
{
	public static byte[] Map = new byte[G.WW * G.WH];

	public static readonly List<Room> Rooms = new List<Room>
	{
		new Room("Шлюз-3 «Відлуння»",        2, 58, 12, 10, 0),
		new Room("Розподільник",              16, 58,  8, 10, 0),
		new Room("Ангар",                    26, 54, 24, 14, 0),
		new Room("Енергокоридор",            26, 42, 24,  8, 0),
		new Room("Медичний блок",            52, 58, 14, 10, 0),
		new Room("Склад життєзабезпечення",  68, 58, 14, 10, 0),
		new Room("Вантажний відсік",         84, 54, 22, 14, 0),
		new Room("Вентиляційний колодязь",   88, 30,  8, 20, 0),
		new Room("Лабораторія «Ковчег»",     80, 16, 24, 12, 0),
		new Room("Східний коридор A",        52, 44, 34,  8, 0),
		new Room("Східний коридор B",        96, 44, 20,  8, 1),
		new Room("Пункт спостереження",     118, 50, 16, 16, 1),
		new Room("Серверна",                118, 34, 16, 12, 1),
		new Room("Реакторний зал",          138, 52, 24, 16, 1),
		new Room("ЯДРО",                    138, 26, 24, 22, 2),
		new Room("Кріогенний відсік",       166, 52, 24, 16, 1),
		new Room("Машинний зал B",          166, 32, 24, 16, 1),
		new Room("Пункт безпеки",            2, 40, 12, 14, 0),
		new Room("Сховище-A",               16, 42,  8,  8, 0),
	};

	static readonly Link[] Links =
	{
		new Link { Ax = 14, Ay = 62, Bx = 16, By = 62 },
		new Link { Ax = 24, Ay = 62, Bx = 26, By = 62 },
		new Link { Ax = 50, Ay = 62, Bx = 52, By = 62 },
		new Link { Ax = 66, Ay = 62, Bx = 68, By = 62 },
		new Link { Ax = 82, Ay = 62, Bx = 84, By = 62 },
		new Link { Ax = 37, Ay = 50, Bx = 37, By = 53 },
		new Link { Ax = 105, Ay = 62, Bx = 118, By = 62 },
		new Link { Ax = 93, Ay = 50, Bx = 93, By = 53, Req = "blue" },
		new Link { Ax = 93, Ay = 28, Bx = 93, By = 29 },
		new Link { Ax = 50, Ay = 46, Bx = 52, By = 46 },
		new Link { Ax = 85, Ay = 48, Bx = 96, By = 48 },
		new Link { Ax = 115, Ay = 45, Bx = 118, By = 45 },
		new Link { Ax = 112, Ay = 46, Bx = 117, By = 50, VertFirst = true },
		new Link { Ax = 133, Ay = 58, Bx = 138, By = 58 },
		new Link { Ax = 161, Ay = 58, Bx = 166, By = 58, Req = "red" },
		new Link { Ax = 178, Ay = 48, Bx = 178, By = 51 },
		new Link { Ax = 150, Ay = 48, Bx = 150, By = 51, Req = "red" },
		new Link { Ax = 166, Ay = 40, Bx = 162, By = 40 },
		new Link { Ax = 8, Ay = 54, Bx = 8, By = 57, Req = "blue" },
		new Link { Ax = 13, Ay = 46, Bx = 16, By = 46, Req = "blue" },
		new Link { Ax = 25, Ay = 46, Bx = 23, By = 46, Req = "blue" },
	};

	static readonly Gate[] GateDefs =
	{
		new Gate { Req = "blue", Pos = new Vector2(93 * G.T + 16, 52 * G.T), Tiles = { new Vector2i(92, 52), new Vector2i(93, 52), new Vector2i(94, 52) } },
		new Gate { Req = "red",  Pos = new Vector2(163 * G.T + 16, 58 * G.T), Tiles = { new Vector2i(163, 57), new Vector2i(163, 58), new Vector2i(163, 59) } },
		new Gate { Req = "red",  Pos = new Vector2(150 * G.T + 16, 50 * G.T), Tiles = { new Vector2i(149, 50), new Vector2i(150, 50), new Vector2i(151, 50) } },
		new Gate { Req = "blue", Pos = new Vector2(8 * G.T + 16, 56 * G.T), Tiles = { new Vector2i(7, 56), new Vector2i(8, 56), new Vector2i(9, 56) } },
		new Gate { Req = "blue", Pos = new Vector2(15 * G.T + 16, 46 * G.T), Tiles = { new Vector2i(15, 45), new Vector2i(15, 46), new Vector2i(15, 47) } },
		new Gate { Req = "blue", Pos = new Vector2(24 * G.T + 16, 46 * G.T), Tiles = { new Vector2i(24, 45), new Vector2i(24, 46), new Vector2i(24, 47) } },
	};

	static readonly PlatDef[] Plats =
	{
		new PlatDef(36, 53, 3), new PlatDef(36, 51, 3),
		new PlatDef(92, 53, 3), new PlatDef(92, 51, 3),
		new PlatDef(177, 51, 3), new PlatDef(177, 49, 3),
		new PlatDef(149, 51, 3), new PlatDef(149, 49, 3),
		new PlatDef(7, 57, 3), new PlatDef(7, 55, 3),

		new PlatDef(6, 66, 5), new PlatDef(6, 64, 5), new PlatDef(6, 62, 5), new PlatDef(6, 60, 5), new PlatDef(6, 58, 5),

		new PlatDef(28, 66, 6), new PlatDef(38, 66, 6), new PlatDef(28, 64, 6), new PlatDef(38, 64, 6),
		new PlatDef(30, 62, 6), new PlatDef(40, 62, 6), new PlatDef(28, 60, 6), new PlatDef(40, 60, 6),
		new PlatDef(36, 58, 5), new PlatDef(34, 56, 6), new PlatDef(35, 54, 4),

		new PlatDef(34, 50, 5), new PlatDef(28, 48, 5), new PlatDef(36, 48, 5), new PlatDef(42, 48, 5),
		new PlatDef(28, 46, 5), new PlatDef(36, 46, 5), new PlatDef(42, 46, 5),

		new PlatDef(54, 66, 4), new PlatDef(54, 64, 4), new PlatDef(57, 62, 4), new PlatDef(60, 64, 4),
		new PlatDef(70, 66, 5), new PlatDef(70, 64, 5), new PlatDef(76, 64, 5), new PlatDef(76, 62, 5),

		new PlatDef(86, 66, 5), new PlatDef(94, 66, 5), new PlatDef(86, 64, 5), new PlatDef(94, 64, 5),
		new PlatDef(90, 62, 6), new PlatDef(98, 62, 5), new PlatDef(91, 60, 5), new PlatDef(98, 60, 5),
		new PlatDef(91, 58, 5), new PlatDef(91, 56, 5), new PlatDef(91, 54, 5),

		new PlatDef(89, 49, 5), new PlatDef(88, 47, 8), new PlatDef(88, 45, 8), new PlatDef(88, 43, 8),
		new PlatDef(88, 41, 8), new PlatDef(88, 39, 8), new PlatDef(88, 37, 8), new PlatDef(88, 35, 8),
		new PlatDef(88, 33, 8), new PlatDef(88, 31, 8), new PlatDef(92, 29, 3),

		new PlatDef(82, 27, 5), new PlatDef(98, 27, 5), new PlatDef(86, 25, 5), new PlatDef(94, 25, 5),
		new PlatDef(82, 23, 5), new PlatDef(98, 23, 5), new PlatDef(86, 21, 5), new PlatDef(94, 21, 5),
		new PlatDef(90, 19, 6), new PlatDef(86, 17, 5),

		new PlatDef(54, 50, 5), new PlatDef(62, 50, 5), new PlatDef(70, 50, 5), new PlatDef(78, 50, 5),
		new PlatDef(54, 48, 5), new PlatDef(62, 48, 5), new PlatDef(70, 48, 5), new PlatDef(78, 48, 5),
		new PlatDef(98, 50, 5), new PlatDef(106, 50, 5), new PlatDef(98, 48, 5), new PlatDef(106, 48, 5),

		new PlatDef(120, 64, 5), new PlatDef(128, 64, 5), new PlatDef(120, 62, 5), new PlatDef(128, 62, 5),
		new PlatDef(124, 60, 5), new PlatDef(124, 58, 5), new PlatDef(120, 56, 5), new PlatDef(128, 56, 5),
		new PlatDef(120, 45, 4), new PlatDef(120, 43, 4), new PlatDef(130, 43, 4),
		new PlatDef(124, 41, 6), new PlatDef(124, 39, 6),

		new PlatDef(140, 66, 6), new PlatDef(146, 66, 5), new PlatDef(151, 66, 6),
		new PlatDef(140, 64, 6), new PlatDef(146, 64, 5), new PlatDef(151, 64, 6),
		new PlatDef(140, 62, 6), new PlatDef(146, 62, 5), new PlatDef(151, 62, 6),
		new PlatDef(140, 60, 6), new PlatDef(146, 60, 5), new PlatDef(151, 60, 6),
		new PlatDef(140, 58, 6), new PlatDef(146, 58, 5), new PlatDef(151, 58, 6),
		new PlatDef(141, 56, 6), new PlatDef(147, 56, 5), new PlatDef(151, 56, 6),
		new PlatDef(148, 52, 4), new PlatDef(152, 52, 6),

		new PlatDef(140, 46, 5), new PlatDef(155, 46, 5), new PlatDef(140, 44, 5), new PlatDef(155, 44, 5),
		new PlatDef(147, 42, 5),

		new PlatDef(168, 66, 5), new PlatDef(176, 66, 5), new PlatDef(170, 64, 6), new PlatDef(176, 64, 5),
		new PlatDef(170, 62, 6), new PlatDef(176, 62, 5), new PlatDef(170, 60, 6), new PlatDef(176, 60, 5),
		new PlatDef(170, 58, 6), new PlatDef(176, 58, 5), new PlatDef(170, 56, 6), new PlatDef(176, 56, 5),
		new PlatDef(172, 54, 6), new PlatDef(177, 52, 4),

		new PlatDef(168, 46, 5), new PlatDef(178, 46, 5), new PlatDef(168, 44, 5), new PlatDef(178, 44, 5),
		new PlatDef(172, 42, 6), new PlatDef(182, 42, 5), new PlatDef(172, 40, 6), new PlatDef(182, 40, 5),
		new PlatDef(168, 38, 5), new PlatDef(182, 38, 5), new PlatDef(172, 36, 6),

		new PlatDef(4, 52, 5), new PlatDef(10, 52, 5), new PlatDef(4, 50, 5), new PlatDef(10, 50, 5),
		new PlatDef(6, 48, 5), new PlatDef(6, 47, 5),
		new PlatDef(18, 48, 4), new PlatDef(18, 46, 4),
	};

	static readonly HazardDef[] Hazards =
	{
		new HazardDef(168, 67, 6, 1),
		new HazardDef(182, 67, 6, 1),
		new HazardDef(138, 47, 4, 1),
		new HazardDef(158, 47, 4, 1),
		new HazardDef(152, 67, 6, 1),
		new HazardDef(66, 51, 5, 1),
	};

	public static readonly ItemDef[] Items =
	{
		new ItemDef("pistol_ammo", 4, 68, 24),
		new ItemDef("pistol_ammo", 11, 68, 24),
		new ItemDef("medkit", 6, 68),

		new ItemDef("rifle", 36, 56),
		new ItemDef("rifle_ammo", 30, 64, 90),
		new ItemDef("rifle_ammo", 40, 64, 90),
		new ItemDef("rifle_ammo", 42, 62, 60),
		new ItemDef("grenade", 34, 68, 2),

		new ItemDef("pistol_ammo", 28, 46, 24),
		new ItemDef("rifle_ammo", 42, 46, 90),
		new ItemDef("medkit", 44, 46),

		new ItemDef("medkit", 56, 68),
		new ItemDef("grenade", 58, 62, 2),

		new ItemDef("rifle_ammo", 72, 64, 90),
		new ItemDef("medkit", 72, 68),
		new ItemDef("pistol_ammo", 78, 68, 36),

		new ItemDef("rifle_ammo", 88, 64, 90),
		new ItemDef("grenade", 92, 62, 3),
		new ItemDef("rifle_ammo", 96, 64, 90),
		new ItemDef("medkit", 93, 60),
		new ItemDef("rifle_ammo", 100, 60, 120),
		new ItemDef("medkit", 104, 68),

		new ItemDef("grenade", 91, 49, 2),
		new ItemDef("rifle_ammo", 92, 47, 90),

		new ItemDef("medkit", 84, 27),
		new ItemDef("rifle_ammo", 100, 23, 120),
		new ItemDef("grenade", 96, 21, 2),
		new ItemDef("double_jump", 92, 19),

		new ItemDef("rifle_ammo", 62, 48, 90),
		new ItemDef("pistol_ammo", 78, 48, 24),
		new ItemDef("medkit", 106, 52),

		new ItemDef("rifle_ammo", 122, 64, 90),
		new ItemDef("grenade", 126, 60, 2),
		new ItemDef("medkit", 122, 56),

		new ItemDef("rifle_ammo", 121, 43, 90),
		new ItemDef("blue", 122, 43),
		new ItemDef("grenade", 126, 39, 2),
		new ItemDef("medkit", 130, 46),

		new ItemDef("rifle_ammo", 142, 66, 90),
		new ItemDef("rifle_ammo", 143, 58, 60),
		new ItemDef("medkit", 148, 58),
		new ItemDef("red", 154, 52),

		new ItemDef("pistol_ammo", 170, 66, 36),
		new ItemDef("rifle_ammo", 178, 66, 120),
		new ItemDef("grenade", 172, 64, 3),
		new ItemDef("rifle_ammo", 172, 60, 120),
		new ItemDef("medkit", 172, 58),
		new ItemDef("rifle_ammo", 178, 56, 90),

		new ItemDef("rifle_ammo", 170, 44, 120),
		new ItemDef("grenade", 174, 42, 2),
		new ItemDef("medkit", 184, 42),
		new ItemDef("rifle_ammo", 170, 38, 90),

		new ItemDef("dash", 6, 47),
		new ItemDef("rifle_ammo", 18, 46, 90),
		new ItemDef("medkit", 20, 50),
	};

	public static readonly StationDef[] Stations =
	{
		new StationDef("health", 63, 68),
		new StationDef("ammo", 80, 68),
		new StationDef("health", 103, 28),
		new StationDef("health", 187, 48),
		new StationDef("ammo", 185, 48),
		new StationDef("ammo", 12, 68),
	};

	public static readonly SpawnDef[] Spawns =
	{
		new SpawnDef("crawler", 2, 2),
		new SpawnDef("brute", 2, 1),
		new SpawnDef("crawler", 3, 1),
		new SpawnDef("crawler", 4, 1),
		new SpawnDef("spitter", 4, 1),
		new SpawnDef("crawler", 5, 2),
		new SpawnDef("crawler", 6, 3),
		new SpawnDef("brute", 6, 1),
		new SpawnDef("crawler", 7, 1),
		new SpawnDef("crawler", 8, 2),
		new SpawnDef("spitter", 8, 1),
		new SpawnDef("crawler", 17, 2),
		new SpawnDef("crawler", 18, 1),
		new SpawnDef("crawler", 9, 2),
		new SpawnDef("crawler", 10, 2),
		new SpawnDef("crawler", 11, 2),
		new SpawnDef("brute", 11, 1),
		new SpawnDef("spitter", 11, 1),
		new SpawnDef("crawler", 12, 2),
		new SpawnDef("spitter", 12, 2),
		new SpawnDef("crawler", 13, 3),
		new SpawnDef("brute", 13, 2),
		new SpawnDef("spitter", 13, 2),
		new SpawnDef("crawler", 15, 3),
		new SpawnDef("brute", 15, 1),
		new SpawnDef("crawler", 16, 2),
		new SpawnDef("spitter", 16, 2),
	};

	public static readonly Vector2 StartPos = new Vector2(5 * G.T, 68 * G.T);

	public static void Build()
	{
		Map = new byte[G.WW * G.WH];
		Gates.Clear();

		for (int i = 0; i < Map.Length; i++) Map[i] = (byte)Tile.Solid;

		foreach (Room r in Rooms)
			for (int y = r.Y; y <= r.Y2; y++)
				for (int x = r.X; x <= r.X2; x++)
					Set(x, y, (int)Tile.Empty);

		foreach (Link l in Links) Carve(l);

		foreach (Gate g in GateDefs)
		{
			Gates.Add(g);
			foreach (Vector2i t in g.Tiles) Set(t.X, t.Y, (int)Tile.Gate);
		}

		foreach (PlatDef p in Plats)
			for (int x = p.X; x < p.X + p.W; x++)
				Set(x, p.Y, (int)Tile.Plat);

		foreach (HazardDef h in Hazards)
			for (int y = h.Y; y < h.Y + h.H; y++)
				for (int x = h.X; x < h.X + h.W; x++)
					Set(x, y, (int)Tile.Hazard);
	}

	static void Carve(Link l)
	{
		int hx1 = Mathf.Min(l.Ax, l.Bx), hx2 = Mathf.Max(l.Ax, l.Bx);
		int vy1 = Mathf.Min(l.Ay, l.By), vy2 = Mathf.Max(l.Ay, l.By);
		bool horizFirst = !l.VertFirst && (Mathf.Abs(l.Bx - l.Ax) >= Mathf.Abs(l.By - l.Ay));

		if (horizFirst)
		{
			CarveH(hx1, hx2, l.Ay);
			CarveV(vy1, vy2, (l.Ax + l.Bx) / 2);
		}
		else
		{
			CarveV(vy1, vy2, l.Ax);
			CarveH(hx1, hx2, (l.Ay + l.By) / 2);
		}
	}

	static void CarveH(int x1, int x2, int y)
	{
		for (int x = x1; x <= x2; x++)
			for (int d = -1; d <= 1; d++)
				Set(x, y + d, (int)Tile.Empty);
	}

	static void CarveV(int y1, int y2, int x)
	{
		for (int y = y1; y <= y2; y++)
			for (int d = -1; d <= 1; d++)
				Set(x + d, y, (int)Tile.Empty);
	}

	public static void Set(int x, int y, int v)
	{
		if (x < 0 || y < 0 || x >= G.WW || y >= G.WH) return;
		Map[y * G.WW + x] = (byte)v;
	}

	public static Tile Get(int x, int y)
	{
		if (x < 0 || y < 0 || x >= G.WW || y >= G.WH) return Tile.Solid;
		return (Tile)Map[y * G.WW + x];
	}

	public static bool Blocked(int x, int y)
	{
		Tile t = Get(x, y);
		return t == Tile.Solid || t == Tile.Gate;
	}

	public static void OpenGate(Gate g)
	{
		if (g.Open) return;
		g.Open = true;
		foreach (Vector2i t in g.Tiles) Set(t.X, t.Y, (int)Tile.Empty);
	}

	public static Room RoomAt(Vector2 p)
	{
		foreach (Room r in Rooms)
			if (r.Contains(p)) return r;
		return null;
	}

	public static bool IsHazard(Rect2 box)
	{
		int x0 = (int)Mathf.Floor(box.Position.X / G.T), x1 = (int)Mathf.Floor((box.End.X - 0.01f) / G.T);
		int y0 = (int)Mathf.Floor(box.Position.Y / G.T), y1 = (int)Mathf.Floor((box.End.Y - 0.01f) / G.T);
		for (int y = y0; y <= y1; y++)
			for (int x = x0; x <= x1; x++)
				if (Get(x, y) == Tile.Hazard) return true;
		return false;
	}

	public static int TotalInfected
	{
		get
		{
			int n = 0;
			foreach (SpawnDef s in Spawns) n += s.Count;
			return n;
		}
	}
}