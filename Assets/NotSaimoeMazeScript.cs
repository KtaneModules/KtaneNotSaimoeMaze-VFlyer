using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;

public class NotSaimoeMazeScript : MonoBehaviour {
	public KMBombModule modSelf;
	public KMAudio MAudio;
	public KMSelectable[] directionSelectables;
	public KMSelectable submitSelectable;
	public SpriteRenderer[] spriteRenderers;
	public Sprite[] allPossibleSprites;
	public string[] spriteNames;
	public TextAsset idxOrder;
	public TextMesh debugText;

	List<int> storedSpriteIdxOrder, selectedIdxOrder;
	int[][] idxGrid;
	const int rowCount = 5, colCount = 5;
	const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	readonly int[] defaultSolveOrderIdxes = new[] { 197, 183, 176, 125, 153, 139, 198, 48, 85, 185, 16, 189, 168, 124, 137, 117, 109, 149, 40, 63, 80, 54, 71, 59, 128, 41, 43, 164, 165, 65, 106, 33, 84, 62, 88, 0, 2, 26, 174, 37, 45, 49, 100, 19, 158, 9, 192, 182, 34, 161, 196, 61, 72, 190, 160, 76, 177, 78, 102, 81, 145, 56, 74, 118, 105, 195, 138, 79, 32, 180, 99, 132, 14, 188, 92, 67, 28, 141, 7, 75, 171, 24, 90, 15, 129, 107, 167, 112, 142, 47, 29, 68, 148, 116, 156, 44, 83, 42, 70, 38, 52, 133, 57, 193, 122, 97, 172, 111, 69, 147, 144, 130, 22, 60, 98, 113, 82, 13, 8, 115, 51, 166, 159, 89, 64, 123, 143, 18, 127, 150, 95, 104, 55, 136, 73, 23 };
	static int moduleIDCnt;
	int curColIdx, curRowIdx, curSubmitIdx, moduleID;
	bool interactable, holdingSubmit, holdingSubmitWhileInteractable, moduleSolved, hideRenderers;
	[SerializeField]
	bool testSprites;
	float timeHeld;
	void QuickLog(string toLog, params object[] args)
    {
		Debug.LogFormat("[Not Saimoe Maze #{0}] {1}", moduleID, string.Format(toLog, args));
    }
	// Use this for initialization
	void Start () {
		moduleID = ++moduleIDCnt;
		storedSpriteIdxOrder = new List<int>();
		if (idxOrder != null)
		{
			var storedIdxPossibleNums = idxOrder.text.Split('\n');
			foreach (var aNum in storedIdxPossibleNums)
			{
				int u;
				if (int.TryParse(aNum, out u))
					storedSpriteIdxOrder.Add(u);
				else
					Debug.LogWarningFormat("\"{0}\" cannot be converted to a valid idx.", aNum);
			}
		}
		else
			storedSpriteIdxOrder.AddRange(defaultSolveOrderIdxes);
		Debug.Log(storedSpriteIdxOrder.Join(", "));
		Debug.Log(storedSpriteIdxOrder.Select(a => allPossibleSprites[a].name).Join(", "));
		spriteNames = allPossibleSprites.Select(a => TryProperName(a.name)).ToArray();

		
        for (var x = 0; x < directionSelectables.Length; x++)
        {
            var y = x;
			directionSelectables[x].OnInteract += delegate { HandleDirectionPress(y); return false; };
        }
		submitSelectable.OnInteract += delegate {
			holdingSubmit = true;
			holdingSubmitWhileInteractable = interactable;
			timeHeld = 0f;
			if (interactable)
				MAudio.PlaySoundAtTransform("Tap", submitSelectable.transform);
			return false;
		};
		submitSelectable.OnInteractEnded += delegate {
			holdingSubmit = false;
			if (holdingSubmitWhileInteractable)
				HandleSubmit();
		};
		//StartCoroutine(HandleResetAnim());
		idxGrid = new int[rowCount][];
		ResetModule();
		interactable = true;
	}
	void HandleSubmit()
    {
		if (!interactable) return;
		if (timeHeld >= 1f)
        {
			interactable = false;
			QuickLog("Requesting a reset. This will take 10-15 seconds.");
			StartCoroutine(HandleResetAnim());
        }
		else
        {
			if (testSprites)
            {
				interactable = false;
				modSelf.HandlePass();
				MAudio.PlaySoundAtTransform("Click", submitSelectable.transform);
				UpdateRenderers(false);
				return;
			}
			if (selectedIdxOrder[curSubmitIdx] == curRowIdx * colCount + curColIdx)
            {
				QuickLog("Correctly submitted {0}{1}{2} Correct submissions so far: {3}", alphabet[curColIdx], curRowIdx + 1, hideRenderers ? "." : "; Careful. The displays are now hidden.", curSubmitIdx + 1);
				hideRenderers = true;
				curSubmitIdx++;
				if (selectedIdxOrder.Count <= curSubmitIdx)
                {
					QuickLog("That's all of them. Module disarmed.");
					interactable = false;
					moduleSolved = true;
					modSelf.HandlePass();
					MAudio.PlaySoundAtTransform("Click", submitSelectable.transform);
				}
			}
			else
            {
				QuickLog("Incorrectly submitted {0}{1}{2} Correct submissions so far: {3}", alphabet[curColIdx], curRowIdx + 1, hideRenderers ? "; Revealing displays again." : "", curSubmitIdx);
				hideRenderers = false;
				modSelf.HandleStrike();
			}
			UpdateRenderers(!hideRenderers);
		}
    }
	void HandleDirectionPress(int idx)
    {
		if (!interactable) return;
		directionSelectables[idx].AddInteractionPunch(0.2f);
		MAudio.PlaySoundAtTransform("Tap", directionSelectables[idx].transform);
		switch (idx)
        {
			case 0:
				curRowIdx = (curRowIdx - 1 + rowCount) % rowCount;
				break;
			case 2:
				curRowIdx = (curRowIdx + 1) % rowCount;
				break;
			case 1:
				curColIdx = (curColIdx + 1) % colCount;
				break;
			case 3:
				curColIdx = (curColIdx - 1 + colCount) % colCount;
				break;
        }
		if (testSprites)
        {
			switch (idx)
			{
				case 0:
					curSubmitIdx -= 10;
					break;
				case 2:
					curSubmitIdx += 10;
					break;
				case 3:
					curSubmitIdx -= 1;
					break;
				case 1:
					curSubmitIdx += 1;
					break;
			}
			curSubmitIdx = (curSubmitIdx + allPossibleSprites.Length) % allPossibleSprites.Length;
		}
		UpdateRenderers(!hideRenderers);
	}
	string TryProperName(string toConvert)
    {
		try
		{
			var blacklistWords = new[] { "of", "the", "to" };
			var firstConversion = toConvert.Replace("_bwTrans", "");
			var output = "";
			string[] words = firstConversion.Split(new[] { '_' }, System.StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < words.Length; i++)
			{
				string portion = words[i];
				if (portion.Length <= 1)
					output += portion.ToUpperInvariant() + " ";
				else if (!blacklistWords.Any(a => a.EqualsIgnoreCase(portion)))
				{
					var firstUppercaseStr = portion.Substring(0, 1).ToUpperInvariant();
					output += firstUppercaseStr + portion.Substring(1) + " ";
				}
				else
					output += portion + " ";
			}
			return output.Trim();
		}
		catch
        {
			return toConvert;
        }
    }
	void ResetModule()
    {
		if (storedSpriteIdxOrder.Any())
        {
			for (var x = 0; x < rowCount; x++)
				if (idxGrid[x] == null)
					idxGrid[x] = new int[colCount];
			var allPossibleDecoyIdxes = Enumerable.Range(0, allPossibleSprites.Length).Where(a => !storedSpriteIdxOrder.Contains(a));
			var selectedSpriteIdxOrder = Enumerable.Range(0, storedSpriteIdxOrder.Count).ToArray().Shuffle().Take(Random.Range(9, 17)).OrderBy(a => a).Select(a => storedSpriteIdxOrder[a]).ToList();
			var allItems = new List<int>();
			allItems.AddRange(selectedSpriteIdxOrder);
			allItems.AddRange(allPossibleDecoyIdxes.ToArray().Shuffle().Take(25 - selectedSpriteIdxOrder.Count));
			allItems.Shuffle();
			selectedIdxOrder = selectedSpriteIdxOrder.Select(a => allItems.IndexOf(a)).ToList();
            for (var x = 0; x < rowCount * colCount; x++)
				idxGrid[x / colCount][x % colCount] = allItems[x];
			QuickLog("The grid now displays pictures of the following franchises/shows:");
			for (var x = 0; x < rowCount; x++)
				QuickLog("{0}", idxGrid[x].Select(a => spriteNames[a]).Join(", "));
			curColIdx = Random.Range(0, colCount);
			curRowIdx = Random.Range(0, rowCount);
			QuickLog("Starting on {0}{1}.", alphabet[curColIdx], curRowIdx + 1);
			QuickLog("Submit the following coordinates in this order: {0}", selectedIdxOrder.Select(a => string.Format("{0}{1}",alphabet[a % colCount], a / colCount + 1)).Join(", "));
		}
		UpdateRenderers();
    }
	IEnumerator HandleResetAnim()
    {
		int resetTime = Random.Range(10, 16);
		for (int y = 0; y < resetTime; y++)
		{
			for (int x = 0; x < spriteRenderers.Length; x++)
				spriteRenderers[x].sprite = x % 2 == y % 2 ? null : allPossibleSprites.PickRandom();

			if (y != resetTime - 1)
			{
				MAudio.PlaySoundAtTransform("Cycle", transform);
				yield return new WaitForSecondsRealtime(1f);
			}
			else
			{
				interactable = true;
                MAudio.PlaySoundAtTransform("Notify", transform);
				//ResetModule();
				curSubmitIdx = 0;
				hideRenderers = false;
				curColIdx = Random.Range(0, colCount);
				curRowIdx = Random.Range(0, rowCount);
				QuickLog("Submission progress reset. Now on {0}{1}.", alphabet[curColIdx], curRowIdx + 1);
				UpdateRenderers();
			}
		}
    }
	// Update is called once per frame
	void UpdateRenderers(bool renderGrid = true) {
		if (!renderGrid)
        {
			for (var x = 0; x < spriteRenderers.Length; x++)
				spriteRenderers[x].sprite = null;
			return;
		}
		var hDeltas = new[] { 0, 1, 0, colCount - 1 };
		var vDeltas = new[] { rowCount - 1, 0, 1, 0 };
		for (var x = 0; x < spriteRenderers.Length; x++)
		{
			var scanRowIdx = (vDeltas[x] + curRowIdx) % rowCount;
			var scanColIdx = (hDeltas[x] + curColIdx) % colCount;
			spriteRenderers[x].sprite = allPossibleSprites[idxGrid[scanRowIdx][scanColIdx]];
		}
		if (testSprites)
        {
			for (var x = 0; x < spriteRenderers.Length; x++)
				spriteRenderers[x].sprite = allPossibleSprites[curSubmitIdx];
			if (debugText != null)
				debugText.text = curSubmitIdx.ToString("000");
        }
	}
	void Update()
    {
		if (holdingSubmit && timeHeld < 1f)
			timeHeld += Time.deltaTime;
		else if (holdingSubmit)
			UpdateRenderers(false);
    }
#pragma warning disable 414
	private readonly string TwitchHelpMessage = "\"!{0} up/right/down/left/u/r/d/l\" [Moves up/right/down/left, \"move\"/\"m\" can be prepended before the directions to move quicker.] | \"!{0} submit\" [Submits current position] | \"!{0} reset\" [Resets the module's submission progress. This will place you in a different spot!]";
#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
    {
		var moveFast = false;
		var inteCmd = command.Trim();
		if (Regex.IsMatch(inteCmd, @"^submit$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			if (!interactable)
			{
				yield return "sendtochaterror You are unable to interact with the module currently. The command was not processed.";
				yield break;
			}
			yield return null;
			submitSelectable.OnInteract();
			submitSelectable.OnInteractEnded();
		}
		else if (Regex.IsMatch(inteCmd, @"^reset$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			if (!interactable)
			{
				yield return "sendtochaterror You are unable to interact with the module currently. The command was not processed.";
				yield break;
			}
			yield return null;
			submitSelectable.OnInteract();
			yield return new WaitUntil(() => timeHeld >= 1f);
			submitSelectable.OnInteractEnded();
		}
		else
		{
			var rgxMove = Regex.Match(inteCmd, @"^m(ove)?\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			if (rgxMove.Success)
			{
				inteCmd = inteCmd.Replace(rgxMove.Value, "").Trim();
				moveFast = true;
			}
			if (Regex.IsMatch(inteCmd, @"^\s*[uldr\s]+\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
			{
				var usableCommand = inteCmd.Trim().ToLowerInvariant();
				List<int> allPresses = new List<int>();
				foreach (string directionportion in usableCommand.Split())
				{
					foreach (char dir in directionportion)
					{
						switch (dir)
						{
							case 'u':
								allPresses.Add(0);
								break;
							case 'd':
								allPresses.Add(2);
								break;
							case 'l':
								allPresses.Add(3);
								break;
							case 'r':
								allPresses.Add(1);
								break;
							default:
								yield return string.Format("sendtochaterror I do not know what direction \"{0}\" is supposed to be.", dir);
								yield break;
						}
					}
				}
				if (allPresses.Any())
				{
					for (int x = 0; x < allPresses.Count; x++)
					{
						yield return null;
						directionSelectables[allPresses[x]].OnInteract();
						yield return string.Format("trywaitcancel {0} Presses have been canceled after {1} movement(s)!", moveFast ? 0.1f : 1f, x);
					}
				}
			}
			else
			{
				string[] cmdSets = inteCmd.Trim().Split();
				List<KMSelectable> allPresses = new List<KMSelectable>();
				for (int x = 0; x < cmdSets.Length; x++)
				{
					if (Regex.IsMatch(cmdSets[x], @"^\s*u(p)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
					{
						allPresses.Add(directionSelectables[0]);
					}
					else if (Regex.IsMatch(cmdSets[x], @"^\s*d(own)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
					{
						allPresses.Add(directionSelectables[2]);
					}
					else if (Regex.IsMatch(cmdSets[x], @"^\s*l(eft)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
					{
						allPresses.Add(directionSelectables[3]);
					}
					else if (Regex.IsMatch(cmdSets[x], @"^\s*r(ight)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
					{
						allPresses.Add(directionSelectables[1]);
					}
					else
					{
						yield return string.Format("sendtochaterror I do not know what direction \"{0}\" is supposed to be.", cmdSets[x]);
						yield break;
					}
				}
				for (var x = 0; x < allPresses.Count; x++)
				{
					yield return null;
					allPresses[x].OnInteract();
					yield return string.Format("trywaitcancel {0} Presses have been canceled after {1} movement(s)!" ,moveFast ? 0.1f : 1f, x);
				}
				yield break;
			}
		}
	}
	IEnumerator TwitchHandleForcedSolve()
    {
		while (!interactable)
			yield return true;
		while (!moduleSolved)
        {
			while (selectedIdxOrder[curSubmitIdx] != curRowIdx * colCount + curColIdx)
            {
				if (curRowIdx != selectedIdxOrder[curSubmitIdx] / colCount)
				{
					directionSelectables[2].OnInteract();
				}
				else
				{
					directionSelectables[1].OnInteract();
				}
				yield return new WaitForSeconds(0.1f);
            }
			submitSelectable.OnInteract();
			submitSelectable.OnInteractEnded();
			yield return new WaitForSeconds(0.1f);
        }
    }
}
