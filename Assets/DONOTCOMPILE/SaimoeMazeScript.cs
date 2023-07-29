using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class SaimoeMazeScript : MonoBehaviour
{
	public KMAudio Audio;
    public KMBombInfo Bomb;
	public KMBombModule Module;
	
	public GameObject[] Buttons;
	public GameObject Center;
	public Sprite[] Saimoes;
	public AudioClip[] SFX;

	int[] StartingPoint = new int[2], Goal = new int[2];
	string[] ValidCharacters = {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"};
	Coroutine Hold;
	int Timer, Focus;
	bool Interactable = true;
	
	string[][] Maze = new string[][]
	{
		new string[10] {"NW", "NS", "NES", "NW", "N", "NS", "NE", "NWS", "NS", "NE"},
		new string[10] {"SW", "N", "NE", "EW", "EW", "NWS", "SE", "NW", "N", "SE"},
		new string[10] {"NW", "E", "SW", "E", "W", "NS", "NS", "E", "WS", "NE"},
		new string[10] {"SWE", "EW", "WN", "SE", "EW", "NW", "NE", "WS", "NE", "SEW"},
		new string[10] {"NW", "SE", "EW", "WNE", "WE", "WES", "WS", "NE", "WS", "NE"},
		new string[10] {"EW", "WN", "SE", "EW", "WE", "NW", "NE", "EW", "NW", "SE"},
		new string[10] {"ESW", "EW", "NW", "SE", "SW", "ES", "WS", "SE", "WS", "NE"},
		new string[10] {"NW", "ES", "WS", "NS", "NS", "NE", "NW", "NS", "N", "SE"},
		new string[10] {"EW", "NW", "NS", "NS", "NE", "SW", "S", "NE", "EW", "NEW"},
		new string[10] {"EW", "SW", "NS", "NSE", "WE", "NW", "NS", "SE", "EW", "EW"},
		new string[10] {"SW", "N", "NS", "NS", "SE", "SW", "NS", "NE", "EW", "EW"},
		new string[10] {"NW", "E", "NW", "NE", "NW", "SN", "NE", "WE", "SW", "ES"},
		new string[10] {"EW", "EWS", "EW", "WS", "ES", "WN", "SE", "W", "SN", "NE"},
		new string[10] {"SW", "NS", "S", "NS", "NES", "EWS", "NSW", "ES", "NWS", "SE"}
	};
	
	//Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool ModuleSolved;
	
	void Awake()
	{
		moduleId = moduleIdCounter++;
		for (int i = 0; i < Buttons.Length; i++)
		{
			int Press = i;
			Buttons[i].GetComponent<KMSelectable>().OnInteract += delegate ()
			{
				ButtonPress(Press);
				return false;
			};
		}
		Center.GetComponent<KMSelectable>().OnInteract += delegate () { Hold = StartCoroutine(NumDetect()); return false; };
		Center.GetComponent<KMSelectable>().OnInteractEnded += delegate () { if (Hold != null) {StopCoroutine(Hold);} StartCoroutine(SubmitOrLoop());};
	}
	
	void Start()
	{
		int TempCol, TempRow;
		StartingPoint[0] = UnityEngine.Random.Range(0,14);
		StartingPoint[1] = UnityEngine.Random.Range(0,10);
		do
		{
			TempCol = UnityEngine.Random.Range(0,10);
			TempRow = UnityEngine.Random.Range(0,14);
		}
		while (TempCol == StartingPoint[1]  || TempRow == StartingPoint[0]);
		Focus = UnityEngine.Random.Range(0,2);
		for (int x = 0; x < 4; x++)
		{
			Buttons[x].GetComponentInChildren<SpriteRenderer>().sprite = x % 2 == Focus ? x < 2 ? Saimoes[(StartingPoint[0] * 10) + TempCol] : Saimoes[(TempRow * 10) + StartingPoint[1]] : null;
		}
		Goal[0] = 0;
		string Serial = Bomb.GetSerialNumber();
		for (int x = 0; x < Bomb.GetSerialNumber().Length; x++)
		{
			Goal[0] += Array.IndexOf(ValidCharacters, Serial[x].ToString());
		}
		Goal[0] %= 14;
		Goal[1] = Bomb.GetSerialNumberNumbers().First();
		Debug.LogFormat("[Saimoe Maze #{0}] Your goal position: {1}-{2}", moduleId, (Goal[0] + 1).ToString(), (Goal[1] + 1).ToString());
		Debug.LogFormat("[Saimoe Maze #{0}] The position of the white-colored image in the maze: {1}-{2}", moduleId, (StartingPoint[0] + 1).ToString(), (TempCol + 1).ToString());
		Debug.LogFormat("[Saimoe Maze #{0}] The position of the black-colored image in the maze: {1}-{2}", moduleId, (TempRow + 1).ToString(), (StartingPoint[1] + 1).ToString());
		Debug.LogFormat("[Saimoe Maze #{0}] Your current position in the maze: {1}-{2}", moduleId, (StartingPoint[0] + 1).ToString(), (StartingPoint[1] + 1).ToString());
	}
	
	void ButtonPress(int Press)
	{
		Audio.PlaySoundAtTransform(SFX[0].name, transform);
		Buttons[Press].GetComponent<KMSelectable>().AddInteractionPunch(0.2f);
		if (!ModuleSolved && Interactable)
		{
			if (Press == 0 && !Maze[StartingPoint[0]][StartingPoint[1]].Contains('N'))
			{
				StartingPoint[0]--;
				Debug.LogFormat("[Saimoe Maze #{0}] You moved up. Your current position is: {1}-{2}", moduleId, (StartingPoint[0] + 1).ToString(), (StartingPoint[1] + 1).ToString());
			}
			
			else if (Press == 1 && !Maze[StartingPoint[0]][StartingPoint[1]].Contains('E'))
			{
				StartingPoint[1]++;
				Debug.LogFormat("[Saimoe Maze #{0}] You moved right. Your current position is: {1}-{2}", moduleId, (StartingPoint[0] + 1).ToString(), (StartingPoint[1] + 1).ToString());
			}
			
			else if (Press == 2 && !Maze[StartingPoint[0]][StartingPoint[1]].Contains('S'))
			{
				StartingPoint[0]++;
				Debug.LogFormat("[Saimoe Maze #{0}] You moved down. Your current position is: {1}-{2}", moduleId, (StartingPoint[0] + 1).ToString(), (StartingPoint[1] + 1).ToString());
			}
			
			else if (Press == 3 && !Maze[StartingPoint[0]][StartingPoint[1]].Contains('W'))
			{
				StartingPoint[1]--;
				Debug.LogFormat("[Saimoe Maze #{0}] You moved left. Your current position is: {1}-{2}", moduleId, (StartingPoint[0] + 1).ToString(), (StartingPoint[1] + 1).ToString());
			}
			
			else
			{
				Module.HandleStrike();
				switch (Press)
				{
					case 0:
						Debug.LogFormat("[Saimoe Maze #{0}] You tried to move up. Your were unable to do that. Module striked.", moduleId);
						break;
					case 1:
						Debug.LogFormat("[Saimoe Maze #{0}] You tried to move right. Your were unable to do that. Module striked.", moduleId);
						break;
					case 2:
						Debug.LogFormat("[Saimoe Maze #{0}] You tried to move down. Your were unable to do that. Module striked.", moduleId);
						break;
					case 3:
						Debug.LogFormat("[Saimoe Maze #{0}] You tried to move left. Your were unable to do that. Module striked.", moduleId);
						break;
					default:
						break;
				}
			}
		}
	}
    int[] adjacentCell(int[] start, int direction)
    {
        if (direction == 0) return new int[] { start[0] - 1, start[1] };
        else if (direction == 1) return new int[] { start[0], start[1] + 1 };
        else if (direction == 2) return new int[] { start[0] + 1, start[1] };
        else if (direction == 3) return new int[] { start[0], start[1] - 1 };
        else throw new IndexOutOfRangeException();  
    }

    IEnumerator SubmitOrLoop()
	{
		if (!ModuleSolved && Interactable)
		{
			if (Timer != 1)
			{
				if (StartingPoint[0] == Goal[0] && StartingPoint[1] == Goal[1])
				{
					ModuleSolved = true;
					Audio.PlaySoundAtTransform(SFX[1].name, transform);
					Module.HandlePass();
					for (int x = 0; x < 4; x++)
					{
						Buttons[x].GetComponentInChildren<SpriteRenderer>().sprite = null;
					}
					Debug.LogFormat("[Saimoe Maze #{0}] You submitted on the position: {1}-{2}. That was correct. The module solved.", moduleId, (StartingPoint[0] + 1).ToString(), (StartingPoint[1] + 1).ToString());
				}
				
				else
				{
					Debug.LogFormat("[Saimoe Maze #{0}] You submitted on the position: {1}-{2}. That was incorrect. The module striked.", moduleId, (StartingPoint[0] + 1).ToString(), (StartingPoint[1] + 1).ToString());
					Module.HandleStrike();
				}
			}
			
			else
			{
				Interactable = false;
				int Range = UnityEngine.Random.Range(10,16);
				Debug.LogFormat("[Saimoe Maze #{0}] You performed a reset on your maze position.", moduleId);
				for (int y = 0; y < Range; y++)
				{
					int TempCol, TempRow;
					StartingPoint[0] = UnityEngine.Random.Range(0,14);
					StartingPoint[1] = UnityEngine.Random.Range(0,10);
					do
					{
						TempCol = UnityEngine.Random.Range(0,10);
						TempRow = UnityEngine.Random.Range(0,14);
					}
					while (TempCol == StartingPoint[1]  || TempRow == StartingPoint[0]);
					Focus = (Focus + 1) % 2;
					for (int x = 0; x < 4; x++)
					{
						Buttons[x].GetComponentInChildren<SpriteRenderer>().sprite = x % 2 == Focus % 2 ? x < 2 ? Saimoes[(StartingPoint[0] * 10) + TempCol] : Saimoes[(TempRow * 10) + StartingPoint[1]] : null;
					}
					
					if (y != Range - 1)
					{
						Audio.PlaySoundAtTransform(SFX[2].name, transform);
						yield return new WaitForSecondsRealtime(1f);
					}
					
					else
					{
						Interactable = true;
						Audio.PlaySoundAtTransform(SFX[3].name, transform);
						Timer = 0;
						Debug.LogFormat("[Saimoe Maze #{0}] The position of the white-colored image in the maze: {1}-{2}", moduleId, (StartingPoint[0] + 1).ToString(), (TempCol + 1).ToString());
						Debug.LogFormat("[Saimoe Maze #{0}] The position of the black-colored image in the maze: {1}-{2}", moduleId, (TempRow + 1).ToString(), (StartingPoint[1] + 1).ToString());
						Debug.LogFormat("[Saimoe Maze #{0}] Your current position in the maze: {1}-{2}", moduleId, (StartingPoint[0] + 1).ToString(), (StartingPoint[1] + 1).ToString());
					}
				}
			}
		}
	}
	
	IEnumerator NumDetect()
	{
		Audio.PlaySoundAtTransform(SFX[0].name, transform);
		Center.GetComponent<KMSelectable>().AddInteractionPunch(0.2f);
		if (!ModuleSolved && Interactable)
		{
			while (Timer != 1)
			{
				yield return new WaitForSecondsRealtime(1f);
				Timer++;
			}
			
			for (int x = 0; x < 4; x++)
			{
				Buttons[x].GetComponentInChildren<SpriteRenderer>().sprite = null;
			}
		}
	}
	
	//twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"To move in the maze, use the command !{0} press u/r/d/l (This can be performed in a chain) | To submit your position, use the command !{0} submit | To reset your position, use the command !{0} reset.";
    #pragma warning restore 414
	
	IEnumerator ProcessTwitchCommand(string command)
    {
		string[] parameters = command.Split(' ');
		if (Regex.IsMatch(command, @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
            if (!Interactable)
			{
				yield return "sendtochaterror You are unable to interact with the module currently. The command was not processed.";
				yield break;
			}
			Center.GetComponent<KMSelectable>().OnInteract();
			yield return new WaitForSecondsRealtime(0.1f);
			Center.GetComponent<KMSelectable>().OnInteractEnded();
		}
		
		if (Regex.IsMatch(command, @"^\s*reset\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
            if (!Interactable)
			{
				yield return "sendtochaterror You are unable to interact with the module currently. The command was not processed.";
				yield break;
			}
			Center.GetComponent<KMSelectable>().OnInteract();
			yield return new WaitForSecondsRealtime(1.25f);
			Center.GetComponent<KMSelectable>().OnInteractEnded();
		}
		
		if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
			yield return null;
			if (!Interactable)
			{
				yield return "sendtochaterror You are unable to interact with the module currently. The command was not processed.";
				yield break;
			}
			
			if (parameters.Length != 2)
			{
				yield return "sendtochaterror Invalid parameter length. The command was not processed.";
				yield break;
			}
			
			for (int x = 0; x < parameters[1].Length; x++)
			{
				switch (parameters[1][x].ToString().ToLower())
				{
					case "u":
						Buttons[0].GetComponent<KMSelectable>().OnInteract();
						break;
					case "r":
						Buttons[1].GetComponent<KMSelectable>().OnInteract();
						break;
					case "d":
						Buttons[2].GetComponent<KMSelectable>().OnInteract();
						break;
					case "l":
						Buttons[3].GetComponent<KMSelectable>().OnInteract();
						break;
					default:
						yield return "sendtochaterror The module detected an invalid movement. The command was not continued.";
						yield break;
				}
				yield return new WaitForSecondsRealtime(0.1f);
			}
		}
	}
    IEnumerator TwitchHandleForcedSolve()
    {
        while (!Interactable) yield return true;
        if (StartingPoint.SequenceEqual(Goal)) goto Submit;
        string directions = "NESW";
        Queue<int[]> q = new Queue<int[]>();
        List<Movement> allMoves = new List<Movement>();
        q.Enqueue(StartingPoint);
        while (q.Count > 0)
        {
            int[] subject = q.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                if (!Maze[subject[0]][subject[1]].Contains(directions[i]) && !allMoves.Any(x => x.start.SequenceEqual(adjacentCell(subject, i))))
                {
                    q.Enqueue(adjacentCell(subject, i));
                    allMoves.Add(new Movement(subject, adjacentCell(subject, i), i));
                }
            }
            if (subject.SequenceEqual(Goal)) break;
        }
        if (allMoves.Count != 0)
        {
            Movement lastMove = allMoves.First(x => x.end.SequenceEqual(Goal));
            List<Movement> path = new List<Movement>() { lastMove };
            while (!lastMove.start.SequenceEqual(StartingPoint))
            {
                lastMove = allMoves.First(x => x.end.SequenceEqual(lastMove.start));
                path.Add(lastMove);
            }
            path.Reverse();
            foreach (Movement move in path)
            {
                Buttons[move.direction].GetComponent<KMSelectable>().OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
        Submit:
        Center.GetComponent<KMSelectable>().OnInteract();
        yield return new WaitForSeconds(0.1f);
        Center.GetComponent<KMSelectable>().OnInteractEnded();
        yield return new WaitForSeconds(0.1f);

    }
}

public class Movement
{
    public int[] start { get; set; }
    public int[] end { get; set; }
    public int direction { get; set; }

    public Movement(int[] a, int[] b, int c)
    {
        start = a; end = b; direction = c;
    }
}