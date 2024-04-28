using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UI.PiratesOfVoxel.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoxelPlay
{
	public partial class GameUICanvasController : VoxelPlayUI
	{
		public InventoryUI Inventory;
		public SelectedItemUI SelectedItem;

		/// <summary>
		/// Returns true if the console is visible
		/// </summary>
		public override bool IsConsoleVisible
		{
			get
			{
				if (console != null)
					return console.activeSelf;
				return false;
			}
		}

		[NonSerialized]
		public VoxelPlayEnvironment env;

		static char[] SEPARATOR_SPACE = { ' ' };

		StringBuilder sb, sbDebug;
		GameObject console, status, debug;
		Text consoleText, debugText, statusText, fpsText, fpsShadow, initText;
		GameObject  initPanel;
		Transform initProgress;
		string lastCommand;

		InputField inputField;
		bool firstTimeConsole;
		readonly char[] forbiddenCharacters = { '<', '>' };
		Image statusBackground;

		float fpsUpdateInterval = 0.5f;

		// FPS accumulated over the interval
		float fpsAccum;

		// Frames drawn over the interval
		int fpsFrames;

		// Left time for current interval
		float fpsTimeleft;

		private const string LOCALIZE_GOT_KEY = "get";

		private readonly Dictionary<VoxelPlayEnvironment.TextType, string> _localizedText =
			new Dictionary<VoxelPlayEnvironment.TextType, string>()
			{
				{VoxelPlayEnvironment.TextType.Initializing, "initializing"},
				{VoxelPlayEnvironment.TextType.LoadWorld, "loading_header"},
				{VoxelPlayEnvironment.TextType.StartGameHint, "hint_start_game"},
				{VoxelPlayEnvironment.TextType.PlayerJoined, "has_joined"},
				{VoxelPlayEnvironment.TextType.PlayerLeft, "left_the_game"},
			};

		public override void InitUI()
		{
			firstTimeConsole = true;
			lastCommand = "";
			Inventory.Init(transform, VoxelPlayPlayer.instance);
			CheckReferences();
			fpsTimeleft = fpsUpdateInterval;
			fpsFrames = 1000;
		}

		void CheckReferences()
		{
			if (env == null)
			{
				env = VoxelPlayEnvironment.instance;
			}

			sb = new StringBuilder(1000);
			sbDebug = new StringBuilder(1000);

			CheckEventSystem();

			fpsShadow = transform.Find("FPSShadow").GetComponent<Text>();
			fpsText = fpsShadow.transform.Find("FPSText").GetComponent<Text>();
			fpsShadow.gameObject.SetActive(env.showFPS);
			console = transform.Find("Console").gameObject;
			console.GetComponent<Image>().color = env.consoleBackgroundColor;
			consoleText = transform.Find("Console/Scroll View/Viewport/ConsoleText").GetComponent<Text>();
			status = transform.Find("Status").gameObject;
			statusBackground = status.GetComponent<Image>();
			statusBackground.color = env.statusBarBackgroundColor;
			statusText = transform.Find("Status/StatusText").GetComponent<Text>();
			debug = transform.Find("Debug").gameObject;
			debug.GetComponent<Image>().color = env.consoleBackgroundColor;
			debugText = transform.Find("Debug/Scroll View/Viewport/DebugText").GetComponent<Text>();
			inputField = transform.Find("Status/InputField").GetComponent<InputField>();
			inputField.onEndEdit.AddListener(delegate
			{
				UserConsoleCommandHandler();
			});
			
			initPanel = transform.Find("InitPanel").gameObject;
			initProgress = initPanel.transform.Find("Box/Progress").transform;
			initText = initPanel.transform.Find("StatusText").GetComponent<Text>();
		}

		void OnDisable()
		{
			if (inputField != null)
			{
				inputField.onEndEdit.RemoveAllListeners();
			}
		}

		void LateUpdate()
		{
			LateUpdateImpl();
		}

		protected virtual void LateUpdateImpl()
		{
			if (env == null) return;
			VoxelPlayInputController input = env.input;
			if (input == null)
				return;
			if (input.anyKey)
			{
				if (env.enableConsole && input.GetButtonDown(InputButtonNames.Console))
				{
					ToggleConsoleVisibility(!console.activeSelf);
				}
				else if (env.enableDebugWindow && input.GetButtonDown(InputButtonNames.DebugWindow))
				{
					ToggleDebugWindow(!debug.activeSelf);
				}
				else if (input.GetButtonDown(InputButtonNames.Escape))
				{
					if (IsConsoleVisible)
					{
						ToggleConsoleVisibility(false);
					}
					if (IsInventoryVisible)
					{
						ToggleInventoryVisibility(false);
					}
				}
				else if (env.enableInventory && input.GetButtonDown(InputButtonNames.Inventory))
				{

					if (!Inventory.IsInventoryVisible)
					{
						ToggleInventoryVisibility(true);
					}
					else
					{
						InventoryNextPage();
					}
				}
				else if (Input.GetKeyDown(KeyCode.UpArrow) && IsConsoleVisible)
				{
					inputField.text = lastCommand;
					inputField.MoveTextEnd(false);
				}
				else if (Input.GetKeyDown(KeyCode.F8))
				{
					ToggleFPS();
				}
				Inventory.CheckUserInput();
			}

			if (debug.activeSelf)
			{
				UpdateDebugInfo();
			}

			if (fpsText.enabled)
			{
				UpdateFPSCounter();
			}

		}


		void CheckEventSystem()
		{
			EventSystem eventSystem = FindObjectOfType<EventSystem>();
			if (eventSystem == null)
			{
				GameObject prefab = Resources.Load<GameObject>("VoxelPlay/Prefabs/EventSystem");
				if (prefab != null)
				{
					GameObject go = Instantiate(prefab) as GameObject;
					go.name = "EventSystem";
				}
			}
		}

		#region Console

		void PrintKeySheet()
		{
			if (sb.Length > 0)
			{
				sb.AppendLine();
				sb.AppendLine();
			}
			sb.AppendLine("<color=orange>** KEY LIST **</color><");
			AppendValue("W/A/S/D");
			sb.AppendLine(" : Move player (front/left/back/right)");
			AppendValue("F");
			sb.AppendLine(" : Toggle Flight Mode");
			AppendValue("Q/E");
			sb.AppendLine(" : Fly up / down");
			AppendValue("C");
			sb.AppendLine(" : Toggles crouching");
			AppendValue("Left Shift");
			sb.AppendLine(" : Hold while move to run / fly faster");
			AppendValue("T");
			sb.AppendLine(" : Interacts with an object");
			AppendValue("G");
			sb.AppendLine(" : Throws currently selected item");
			AppendValue("L");
			sb.AppendLine(" : Toggles character light");
			AppendValue("Mouse Move");
			sb.AppendLine(" : Look around");
			AppendValue("Mouse Left Button");
			sb.AppendLine(" : Fire / hit blocks");
			AppendValue("Mouse Right Button");
			sb.AppendLine(" : Build blocks");
			AppendValue("Tab");
			sb.AppendLine(" : Show inventory and browse items (Tab / Shift-Tab)");
			AppendValue("Esc");
			sb.AppendLine(" : Closes all windows (inventory, console)");
			AppendValue("B");
			sb.AppendLine(" : Activate Build mode");
			AppendValue("F1");
			sb.AppendLine(" : Show / hide console");
			AppendValue("F2");
			sb.AppendLine(" : Show / hide debug window");
			AppendValue("Control + F3");
			sb.Append(" : Load Game / ");
			AppendValue("Control + F4");
			sb.AppendLine(" : Quick save");
			AppendValue("F8");
			sb.Append(" : Toggle FPS");
			consoleText.text = sb.ToString();
		}

		void PrintCommands()
		{
			if (sb.Length > 0)
			{
				sb.AppendLine();
				sb.AppendLine();
			}
			sb.AppendLine("<color=orange>** COMMAND LIST **</color>");
			AppendValue("/help");
			sb.AppendLine(" : Show this list of commands");
			AppendValue("/keys");
			sb.AppendLine(" : Show available keys and actions");
			AppendValue("/clear");
			sb.AppendLine(" : Clear the console");
			//AppendValue ("/save [filename]");
			//sb.AppendLine (" : Save current game to 'filename' (only filename, no extension)");
			//AppendValue ("/load [filename]");
			//sb.AppendLine (" : Load a previously saved game");
			//AppendValue ("/teleport x y z");
			//sb.AppendLine (" : Instantly teleport player to x y z location");
			//AppendValue ("/stuck");
			//sb.AppendLine (" : Moves player on top of ground");
			AppendValue("/time hh:mm");
			sb.AppendLine(" : Sets time of day in 23:59 hour format");
			AppendValue("/debug");
			sb.AppendLine(" : Shows debug info about the last voxel hit");
			sb.Append("Press <color=yellow>F1</color> again or <color=yellow>ESC</color> to return to game.");

			consoleText.text = sb.ToString();
		}

		void AppendValue(object o)
		{
			sb.Append("<color=yellow>");
			sb.Append(o);
			sb.Append("</color>");
		}


		/// <summary>
		/// Shows/hides the console
		/// </summary>
		/// <param name="state">If set to <c>true</c> state.</param>
		public override void ToggleConsoleVisibility(bool state)
		{
			if (!env.applicationIsPlaying)
				return;

			if (statusText == null)
			{
				CheckReferences();
				if (statusText == null)
					return;
			}

			if (firstTimeConsole)
			{
				firstTimeConsole = false;
				AddConsoleText("<color=green>Enter <color=yellow>/help</color> for a list of commands.</color>");
			}
			status.SetActive(state);
			console.SetActive(state);
			consoleText.fontSize = statusText.fontSize;

			MouseLook.EnableCursor(state);

			if (state)
			{
				ToggleInventoryVisibility(false);
				statusText.text = "";
				FocusInputField();
			}

			VoxelPlayEnvironment.instance.input.enabled = !state;
		}

		/// <summary>
		/// Adds a custom text to the console
		/// </summary>
		public override void AddConsoleText(string text)
		{
			if (sb == null || consoleText == null || !env.enableStatusBar)
				return;
			if (sb.Length > 0)
			{
				sb.AppendLine();
			}
			if (sb.Length > 12000)
			{
				sb.Length = 0;
			}
			sb.Append(text);
			consoleText.text = sb.ToString();
		}

		/// <summary>
		/// Adds a custom message to the status bar and to the console.
		/// </summary>
		public override void AddMessage(string text, float displayTime = 4f, bool flash = true, bool openConsole = false)
		{
			if (!Application.isPlaying || env == null || !env.enableStatusBar)
				return;

			if (statusText == null)
			{
				CheckReferences();
				if (statusText == null)
					return;
			}

			if (text != statusText.text)
			{
				AddConsoleText(text);

				// If console is not shown, only show this message
				if (!console.activeSelf)
				{
					if (openConsole)
					{
						ToggleConsoleVisibility(true);
					}
					else
					{
						statusText.text = text;
						status.SetActive(true);
						CancelInvoke(nameof(HideStatusText));
						Invoke(nameof(HideStatusText), displayTime);
						if (flash)
						{
							StartCoroutine(FlashStatusText());
						}
					}
				}

				ConsoleNewMessage(text);
			}
		}

		public override void AddLocalizedMessage(string text, float displayTime = 4f, bool flash = true,
			bool openConsole = false)
		{
			string localizedText = Localization.Get(text);
			
			AddMessage(localizedText, displayTime, flash, openConsole);
		}

		public override void AddPlayerMessage(string playerName, VoxelPlayEnvironment.TextType textType,
			float displayTime = 4f, bool flash = true, bool openConsole = false)
		{
			string playerStatus = Localization.Get(_localizedText[textType]);

			StringBuilder builder = new StringBuilder();
			string localizedText = builder.Append(playerName).Append(" ").Append(playerStatus).ToString();

			AddMessage(localizedText, displayTime, flash, openConsole);
		}

		public override void AddWelcomeMessage(float displayTime = 4f, bool flash = true, bool openConsole = false)
		{
			string localizedText = Localization.Get(_localizedText[VoxelPlayEnvironment.TextType.StartGameHint]);
			
			AddMessage(localizedText, displayTime, flash, openConsole);
		}

		public override void AddGetItemsMessage(float count, string item, float displayTime = 4f,
			bool flash = true, bool openConsole = false)
		{
			string getText = Localization.Get(LOCALIZE_GOT_KEY);
			string itemText = Localization.Get(item);

			StringBuilder builder = new StringBuilder();
			string localizedText = builder.Append(getText).Append(": ").Append(count).Append("x ").Append(itemText).ToString();

			AddMessage(localizedText, displayTime, flash, openConsole);
		}


		IEnumerator FlashStatusText()
		{
			if (statusBackground == null)
				yield break;
			float startTime = Time.time;
			float elapsed;
			Color startColor = new Color(0, 1.1f, 1.1f, env.statusBarBackgroundColor.a);
			do
			{
				elapsed = Time.time - startTime;
				if (elapsed >= 1f)
					elapsed = 1f;
				if (statusBackground == null)
					yield break;
				statusBackground.color = Color.Lerp(startColor, env.statusBarBackgroundColor, elapsed);
				yield return new WaitForEndOfFrame();
			} while (elapsed < 1f);
		}


		/// <summary>
		/// Hides the status bar
		/// </summary>
		public override void HideStatusText()
		{
			if (statusText != null)
			{
				statusText.text = "";
			}
			if (console != null && console.activeSelf)
			{
				return;
			}
			if (status != null)
			{
				status.SetActive(false);
			}
		}

		void UserConsoleCommandHandler()
		{
			if (inputField == null)
				return;
			string text = inputField.text;
			bool sanitize = false;
			for (int k = 0; k < forbiddenCharacters.Length; k++)
			{
				if (text.IndexOf(forbiddenCharacters[k]) >= 0)
				{
					sanitize = true;
					break;
				}
			}
			if (sanitize)
			{
				string[] temp = text.Split(forbiddenCharacters, StringSplitOptions.RemoveEmptyEntries);
				text = String.Join("", temp);
			}

			if (!string.IsNullOrEmpty(text))
			{
				lastCommand = text;
				if (!ProcessConsoleCommand(text))
				{
					env.ShowMessage(text);
				}
				ConsoleNewCommand(inputField.text);
				if (inputField != null)
				{
					inputField.text = "";
					FocusInputField(); // avoids losing focus
				}
			}
		}

		void FocusInputField()
		{
			if (inputField == null)
				return;
			inputField.ActivateInputField();
			inputField.Select();
		}

		bool ProcessConsoleCommand(string command)
		{
			string upperCommand = command.ToUpper();
			if (upperCommand.IndexOf("/CLEAR") >= 0)
			{
				sb.Length = 0;
				consoleText.text = "";
				return true;
			}
			if (upperCommand.IndexOf("/KEYS") >= 0)
			{
				PrintKeySheet();
				return true;
			}
			if (upperCommand.IndexOf("/HELP") >= 0)
			{
				PrintCommands();
				return true;
			}
			//if (upperCommand.IndexOf("/LOAD") >= 0) {
			//    ProcessLoadCommand(command);
			//    return true;
			//}
			//if (upperCommand.IndexOf("/SAVE") >= 0) {
			//    ProcessSaveCommand(command);
			//    return true;
			//}
			//if (upperCommand.IndexOf("/TELEPORT") >= 0) {
			//    ToggleConsoleVisibility(false);
			//    ProcessTeleportCommand(command);
			//    return true;
			//}
			//if (upperCommand.IndexOf("/STUCK") >= 0)
			//{
			//	if (VoxelPlayFirstPersonController.instance != null)
			//		VoxelPlayFirstPersonController.instance.Unstuck(true);
			//	return true;
			//}
			if (upperCommand.IndexOf("/DEBUG") >= 0)
			{
				ToggleDebugWindow(!debug.activeSelf);
				return true;
			}
			if (upperCommand.IndexOf("/TIME") >= 0)
			{
				ProcessTimeCommand(command);
			}
			return false;
		}

		void ProcessInvokeCommand(string command)
		{
			string[] args = command.Split(SEPARATOR_SPACE, System.StringSplitOptions.RemoveEmptyEntries);
			if (args.Length >= 3)
			{
				string goName = args[1];
				string cmdParams = args[2];
				GameObject go = GameObject.Find(goName);
				if (go == null)
				{
					AddMessage("GameObject '" + goName + "' not found.");
				}
				else
				{
					go.SendMessage(cmdParams, SendMessageOptions.DontRequireReceiver);
					ToggleConsoleVisibility(false);
				}
			}
		}

		void ProcessSaveCommand(string command)
		{
			string[] args = command.Split(SEPARATOR_SPACE, System.StringSplitOptions.RemoveEmptyEntries);
			if (args.Length >= 2)
			{
				string saveFilename = args[1];
				if (!string.IsNullOrEmpty(saveFilename))
				{
					env.saveFilename = args[1];
				}
			}
			env.SaveGameBinary();
		}

		void ProcessLoadCommand(string command)
		{
			string[] args = command.Split(SEPARATOR_SPACE, System.StringSplitOptions.RemoveEmptyEntries);
			if (args.Length >= 2)
			{
				string saveFilename = args[1];
				if (!string.IsNullOrEmpty(saveFilename))
				{
					env.saveFilename = args[1];
				}
			}
			// use invoke to ensure all pending UI events are processed before destroying UI, console, etc. and avoid errors with EventSystem, etc.
			Invoke(nameof(LoadGame), 0.1f);
		}

		void LoadGame()
		{
			if (!env.LoadGameBinary(false))
			{
				AddMessage("<color=red>Load error:</color><color=orange> Game '<color=white>" + env.saveFilename + "</color>' could not be loaded.</color>");
			}
		}

		void ProcessFloodCommand(string command)
		{
			string[] args = command.Split(SEPARATOR_SPACE, System.StringSplitOptions.RemoveEmptyEntries);
			if (args.Length >= 2)
			{
				string mode = args[1].ToUpper();
				env.enableWaterFlood = "ON".Equals(mode);
			}
			AddMessage("<color=green>Flood is <color=yellow>" + (env.enableWaterFlood ? "ON" : "OFF") + "</color></color>");
		}


		void ProcessTeleportCommand(string command)
		{
			try
			{
				string[] args = command.Split(SEPARATOR_SPACE, System.StringSplitOptions.RemoveEmptyEntries);
				if (args.Length >= 3)
				{
					float x = float.Parse(args[1]);
					float y = float.Parse(args[2]);
					float z = float.Parse(args[3]);
					env.characterController.transform.position = new Vector3(x + 0.5f, y, z + 0.5f);
					ToggleConsoleVisibility(false);
				}
			}
			catch
			{
				AddInvalidCommandError();
			}
		}


		void ProcessTimeCommand(string command)
		{
			try
			{
				string[] args = command.Split(SEPARATOR_SPACE, System.StringSplitOptions.RemoveEmptyEntries);
				if (args.Length >= 1)
				{
					string[] t = args[1].Split(new char[] { ':' }, System.StringSplitOptions.RemoveEmptyEntries);
					if (t.Length == 2)
					{
						float hour, minute;
						if (float.TryParse(t[0], out hour) && float.TryParse(t[1], out minute))
						{
							env.SetTimeOfDay(hour + minute / 60f);
						}
					}
				}
			}
			catch
			{
				AddInvalidCommandError();
			}
		}




		void AddInvalidCommandError()
		{
			AddMessage("<color=orange>Invalid command.</color>");
		}

		#endregion

		#region Inventory related
		public override bool IsInventoryVisible => Inventory.IsInventoryVisible;

		/// <summary>
		/// Show/hide inventory
		/// </summary>
		/// <param name="state">If set to <c>true</c> visible.</param>
		public override void ToggleInventoryVisibility(bool state)
		{
			Inventory.ToggleVisibility(state);
		}

		/// <summary>
		/// Advances to next inventory page
		/// </summary>
		public override void InventoryNextPage()
		{
			Inventory.NextPage();
		}

		/// <summary>
		/// Shows previous inventory page
		/// </summary>
		public override void InventoryPreviousPage()
		{
			Inventory.PreviousPage();
		}

		/// <summary>
		/// Refreshs the inventory contents.
		/// </summary>
		public override void RefreshInventoryContents()
		{
			Inventory.RefreshContents();
		}
		#endregion

		#region SelectedItem
		/// <summary>
		/// Updates selected item representation on screen
		/// </summary>
		public override void ShowSelectedItem(InventoryItem inventoryItem)
		{
			SelectedItem.SetData(inventoryItem);
			SelectedItem.Toggle(true);
		}

		/// <summary>
		/// Hides selected item graphic
		/// </summary>
		public override void HideSelectedItem()
		{
			SelectedItem.Toggle(false);
		}
		#endregion

		#region Initialization Panel

		public override void ToggleInitializationPanel(bool visible, string text = "", float progress = 0)
		{
			if (!Application.isPlaying)
			{
				return;
			}

			if (initProgress == null)
			{
				CheckReferences();
			}

			progress = Mathf.Clamp01(progress);

			initProgress.localScale = new Vector3(progress, 1, 1);

			if (visible)
			{
				initText.text = text;
			}

			initPanel.SetActive(visible);
		}

		public override void ToggleLocalizedInitializationPanel(bool visible, VoxelPlayEnvironment.TextType textType, float progress = 0)
		{
			string localizedText = Localization.Get(_localizedText[textType]);

			ToggleInitializationPanel(visible, localizedText, progress);
		}


		#endregion

		#region Debug Window

		public override void ToggleDebugWindow(bool visible)
		{
			debug.SetActive(visible);
		}

		void UpdateDebugInfo()
		{

			sbDebug.Length = 0;

			if (env.playerGameObject != null)
			{
				Vector3 pos = env.playerGameObject.transform.position;
				sbDebug.Append("Player Position: X=");
				AppendValueDebug(pos.x.ToString("F2"));

				sbDebug.Append(", Y=");
				AppendValueDebug(pos.y.ToString("F2"));

				sbDebug.Append(", Z=");
				AppendValueDebug(pos.z.ToString("F2"));
			}

			VoxelChunk currentChunk = env.GetCurrentChunk();
			if (currentChunk != null)
			{

				sbDebug.AppendLine();

				sbDebug.Append("Current Chunk: Id=");
				AppendValueDebug(currentChunk.poolIndex);

				sbDebug.Append(", X=");
				AppendValueDebug(currentChunk.position.x);

				sbDebug.Append(", Y=");
				AppendValueDebug(currentChunk.position.y);

				sbDebug.Append(", Z=");
				AppendValueDebug(currentChunk.position.z);
			}
			VoxelChunk hitChunk = env.lastHitInfo.chunk;
			if (hitChunk != null)
			{
				int voxelIndex = env.lastHitInfo.voxelIndex;

				sbDebug.AppendLine();

				sbDebug.Append("Last Chunk Hit: Id=");
				AppendValueDebug(hitChunk.poolIndex);

				sbDebug.Append(", X=");
				AppendValueDebug(hitChunk.position.x);

				sbDebug.Append(", Y=");
				AppendValueDebug(hitChunk.position.y);

				sbDebug.Append(", Z=");
				AppendValueDebug(hitChunk.position.z);

				sbDebug.Append(", AboveTerrain=");
				AppendValueDebug(hitChunk.isAboveSurface);

				if (hitChunk.modified)
				{
					sbDebug.Append(" (modified)");
				}

				int px, py, pz;
				env.GetVoxelChunkCoordinates(voxelIndex, out px, out py, out pz);

				sbDebug.AppendLine();

				sbDebug.Append("Last Voxel Hit: X=");
				AppendValueDebug(px);

				sbDebug.Append(", Y=");
				AppendValueDebug(py);

				sbDebug.Append(", Z=");
				AppendValueDebug(pz);

				sbDebug.Append(", Index=");
				AppendValueDebug(env.lastHitInfo.voxelIndex);

				sbDebug.Append(", Light=");
				AppendValueDebug(env.lastHitInfo.voxel.lightOrTorch);

				sbDebug.Append(", Light Above=");
				AppendValueDebug(env.GetVoxel(env.lastHighlightInfo.voxelCenter + Misc.vector3up).lightOrTorch);

				if (env.lastHitInfo.voxel.typeIndex != 0)
				{
					sbDebug.AppendLine();
					sbDebug.Append("     Voxel Type=");
					AppendValueDebug(env.lastHitInfo.voxel.type.name);

					sbDebug.Append(", Pos: X=");
					Vector3 v = env.GetVoxelPosition(hitChunk.position, px, py, pz);
					AppendValueDebug(v.x);

					sbDebug.Append(", Y=");
					AppendValueDebug(v.y);

					sbDebug.Append(", Z=");
					AppendValueDebug(v.z);
				}


			}
			debugText.text = sbDebug.ToString();
		}

		void AppendValueDebug(object o)
		{
			sbDebug.Append("<color=yellow>");
			sbDebug.Append(o);
			sbDebug.Append("</color>");
		}

		#endregion

		#region FPS

		void ToggleFPS()
		{
			fpsShadow.gameObject.SetActive(!fpsShadow.gameObject.activeSelf);
		}

		void UpdateFPSCounter()
		{
			fpsTimeleft -= Time.deltaTime;
			fpsAccum += Time.timeScale / Time.deltaTime;
			++fpsFrames;
			if (fpsTimeleft <= 0.0)
			{
				if (fpsText != null && fpsShadow != null)
				{
					int fps = (int)(fpsAccum / fpsFrames);
					fpsText.text = fps.ToString();
					fpsShadow.text = fpsText.text;
					if (fps < 30)
						fpsText.color = Color.yellow;
					else if (fps < 10)
						fpsText.color = Color.red;
					else
						fpsText.color = Color.green;
				}
				fpsTimeleft = fpsUpdateInterval;
				fpsAccum = 0.0f;
				fpsFrames = 0;
			}
		}


		#endregion
	}
}