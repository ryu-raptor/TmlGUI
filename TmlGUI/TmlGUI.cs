using System;
using DxLibDLL;
using System.Collections.Generic;
using System.Xml;
using System.IO;


namespace TmlGUI
{
	public delegate void EventHandler (Control sender, EventArgs args);
	public delegate void EventHandler<T> (Control sender, T args);
	
	public struct pointint
	{
		public int x;
		public int y;

		public pointint (int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public static pointint Get (int x, int y)
		{
			pointint rv;
			rv.x = x;
			rv.y = y;
			return rv;
		}

		public static bool Equals (pointint A, pointint B)
		{
			if ((A.x == B.x) && (A.y == B.y))
				return true;
			else
				return false;
		}

		public static pointint operator + (pointint ope1, pointint ope2)
		{
			pointint rv;
			rv.x = ope1.x + ope2.x;
			rv.y = ope1.y + ope2.y;
			return rv;
		}

		public static pointint operator - (pointint ope1, pointint ope2)
		{
			pointint rv;
			rv.x = ope1.x - ope2.x;
			rv.y = ope1.y - ope2.y;
			return rv;
		}
	}

	public struct rectint
	{
		public pointint TopLeft;
		public pointint BottomRight;

		public rectint (int x1, int y1, int x2, int y2)
		{
			TopLeft = new pointint (x1, y1);
			BottomRight = new pointint (x2, y2);
		}

		public bool Includes (pointint Point)
		{
			if (((Point.x >= TopLeft.x) && (Point.x <= BottomRight.x)) && ((Point.y >= TopLeft.y) && (Point.y <= BottomRight.y))) {
				return true;
			}
			return false;
		}

	}

	public struct GUIMessage : ICloneable
	{
		public int Message;
		public byte[] KeyBuffer;
		public byte[] KeyBufferMask;
		public pointint MousePoint;
		public pointint MouseMove;
		public int MouseClickState;
		public int MouseWheelInput;

		public void AddMessage (int message)
		{
			this.Message = this.Message | message;
			return;
		}

		public void RemoveMessage (int message)
		{
			this.Message = this.Message & (~message);
			return;
		}

		public void CopyTo (out GUIMessage direction)
		{
			direction.Message = Message;
			direction.MousePoint = MousePoint;
			direction.MouseMove = MouseMove;
			direction.MouseClickState = MouseClickState;
			direction.MouseWheelInput = MouseWheelInput;
			direction.KeyBuffer = KeyBuffer.Clone () as byte[];
			direction.KeyBufferMask = KeyBufferMask.Clone () as byte[];
			return;
		}

		public object Clone()
		{
			GUIMessage instance = (GUIMessage)this.MemberwiseClone ();
			instance.KeyBuffer = (byte[])this.KeyBuffer.Clone ();
			instance.KeyBufferMask = (byte[])this.KeyBufferMask.Clone ();
			return instance;
		}
	}

	public class DXEx
	{

		/// <summary>
		/// 指定文字数で折り返す DrawString. \nも使えます
		/// </summary>
		/// <returns>The wrap string.</returns>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="textheight">文字の高さ(改行高さ)</param>
		/// <param name="wraplength">折り返し文字数.</param>
		/// <param name="text">Text.</param>
		/// <param name="textcolor">Textcolor.</param>
		public static int DrawWrapStringToHandle (int x, int y, int textheight, int wraplength, string text, uint textcolor, int FHandle)
		{
			if (text == null)
				return -1;
			//文字列の分割
			var Buffer = GenerateWrapString (wraplength, text);

			//描画
			int rv = 0;
			foreach (var alge in Buffer) {
				rv *= DX.DrawStringToHandle (x, y, alge, textcolor, FHandle) + 1;
				y += textheight;
			}
			return rv - 1;
		}

		/// <summary>
		/// String[] を改行しながら描画します
		/// </summary>
		/// <returns>The wrap string.</returns>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="textheight">Textheight.</param>
		/// <param name="text">GenerateWrapStringなどで生成されたstring配列</param>
		/// <param name="textcolor">Textcolor.</param>
		public static int DrawWrapStringToHandle (int x, int y, int textheight, string[] text, uint textcolor, int FHandle)
		{
			//描画
			int rv = 0;
			foreach (var alge in text) {
				rv *= DX.DrawStringToHandle (x, y, alge, textcolor, FHandle) + 1;
				y += textheight;
			}
			return rv - 1;
		}

		/// <summary>
		/// DrawWrapString関数用のラップ済みテキスト配列を生成します.速度向上にぜひ.
		/// </summary>
		/// <returns>The wrap string.</returns>
		/// <param name="wraplength">折り返しする文字数</param>
		/// <param name="text">Text.</param>
		public static string[] GenerateWrapString (int wraplength, string text)
		{
			if (text == null)
				return null;
			//文字列の分割
			int startp = 0;
			int index = 0;
			string MiniBuf;
			List<string> Buffer = new List<string> ((text.Length / wraplength + 1) * 2);
			Buffer.AddRange (text.Split ('\n'));
			//折り返しの生成
			while (index < Buffer.Count) {
				startp = 0;
				if (Buffer [index].Length <= wraplength) {
					index++;
					continue;
				}

				while (startp < Buffer [index].Length) {
					//1.文字列の抽出
					if (startp + wraplength > Buffer [index].Length) {
						MiniBuf = Buffer [index].Substring (startp);
					} else {
						MiniBuf = Buffer [index].Substring (startp, wraplength);
					}
					Buffer.Insert (index, MiniBuf);
					index++;
					startp += wraplength;
				}
				Buffer.RemoveAt (index);
			}
			//変換して返す
			return Buffer.ToArray ();
		}
	}

	public class EventArgs
	{
	}

	public class MouseArgs : EventArgs
	{
		public pointint MousePoint;
		public pointint MouseMove;
		public int MouseClickState;

		public MouseArgs (pointint mpos, pointint mmov, int mcst)
		{
			MousePoint = mpos;
			MouseMove = mmov;
			MouseClickState = mcst;
		}

		public MouseArgs (pointint mpos, pointint mmov)
		{
			MousePoint = mpos;
			MouseMove = mmov;
		}
	}

	public class ResourceManager
	{
		private static List<Control> Controls;

		/// <summary>
		/// Opens the resource file.
		/// </summary>
		/// <returns>Success = 0, Failed = -1.</returns>
		/// <param name="path">Path to the resource file(.rsc).</param>
		public static int OpenResource (string Xmlpath)
		{
			Controls = new List<Control> ();

			Control Buffer = null;

			XmlReaderSettings setting = new XmlReaderSettings ();
			setting.IgnoreComments = true;
			setting.IgnoreWhitespace = true;
			using (XmlReader Handle = XmlReader.Create (new StreamReader (Xmlpath), setting)) {
				//Skips Headers
				Handle.MoveToContent ();

				while (Handle.Read ()) {
					//XmlGUICreatorと同じような書式で読み込む。ただし入れ子構造は認められない
					//Xml Format is the same as XmlGUICreator, expect child elements is not permitted.
					if ((Handle.NodeType != XmlNodeType.Element) || (!Handle.IsEmptyElement)) {
						continue;
					}

					//実体化とマージン等の設定
					//Make an instance and get settings of margins.
					switch (Handle.Name) {
					case "Form":
						Buffer = new Form ();
						(Buffer as Form).TitleBarMargin = XmlGUICreator.Parse (Handle.GetAttribute ("TitleBarMargin"), 4);
						(Buffer as Form).TitleBarHeight = XmlGUICreator.Parse (Handle.GetAttribute ("TitleBarHeight"), 16);
						break;
					case "Button":
						Buffer = new Button ();
						break;
					case "TextBox":
						Buffer = new TextBox ();
						break;
					case "Label":
						Buffer = new Label (10, 10, "");
						break;
					case "ListControl":
						Buffer = new ListControl<int> (1);
						(Buffer as ListControl<int>).CaptionMargin = XmlGUICreator.Parse (Handle.GetAttribute ("CaptionMargin"), (Buffer as ListControl<int>).CaptionMargin);
						(Buffer as ListControl<int>).ThumbnailMargin = XmlGUICreator.Parse (Handle.GetAttribute ("ThumbnailMargin"), (Buffer as ListControl<int>).ThumbnailMargin);
						(Buffer as ListControl<int>).ItemHeight = XmlGUICreator.Parse (Handle.GetAttribute ("ItemHeight"), (Buffer as ListControl<int>).ItemHeight);
						break;
					default:
						Console.WriteLine ("TmlGUI.ResourceManager : Erorr : Invalid Control name : {0}", Handle.Name);
						continue;
					}

					Buffer.Text = "Default instance";
					Buffer.BaseMargin = XmlGUICreator.Parse (Handle.GetAttribute ("BaseMargin"), 4);

					//スキンのセット
					//Setting Skin
					if (Handle.GetAttribute ("Skin") == null) {
						Console.WriteLine ("TmlGUI.ResourceManager : Erorr : There is no Skin attribute in {0}", Handle.Name);
						continue;
					} else {
						Buffer.SetSkin (Handle.GetAttribute ("Skin"));
					}

					//子コントロールに代入
					//Add Buffer to Controls
					Controls.Add (Buffer);
					Buffer = null;
				}
			}
			return 0;
		}

		/// <summary>
		/// DON'T USE THIS METHOD. Copies the skin.
		/// </summary>
		/// <param name="dist">スキンのコピー先.</param>
		public void CopySkin(Control dist)
		{
		}

		/// <summary>
		/// Gets the copy of the instance created by ResourceManager.
		/// </summary>
		/// <returns>The control's default instance of ResourceManager.</returns>
		/// <param name="control">Control that is type you want to get the default instance of.</param>
		public static Control GetDefault(Control control)
		{
			return GetDefault (control.GetType ());
		}

		/// <summary>
		/// Gets the copy of the instance created by ResourceManager.
		/// </summary>
		/// <returns>The default.</returns>
		/// <param name="type">Type you want to get the default instance of.</param>
		public static Control GetDefault(Type type)
		{
			return Controls.Find (((Control obj) => obj.GetType ().Name == type.Name)).Clone () as Control;
		}
	}

	public class GraphicalUI
	{
		public const int Message_Cursor_Move = 1;
		public const int Message_Mouse_Click = 2;
		public const int Message_MouseWheel_Role = 4;
		public const int Message_Keyboard_Down = 8;
		public const int Message_Keyboard_Up = 16;
		public const int Message_Mouse_LeftClick = 32;
		public const int Message_Mouse_RightClick = 64;
		public const int Message_Mouse_MiddleClick = 128;
		
		private static Func<pointint> MousePointGettingMethod;
		private static Func<int> MouseClickGettingMethod;
		private static Func<int> MouseWheelGettingMethod;
		private static Func<byte[]> KeyboardGettingMethod;
		private static Func<int,bool> KeyboardStateGettingMethod;
		private static Func<int,int> JoypadStateGettingMethod;

		public static int TitleBarFont { get; private set; } = DX.GetDefaultFontHandle();

		public static int BaseFont { get; private set; } = TitleBarFont;

		public static int ButtonFont { get; private set; } = TitleBarFont;

		private static List<Form> Controls = new List<Form> ();

		public static Form Parent { get; private set; }

		private static GUIMessage Message;

		public GraphicalUI ()
		{
		}

		public static void GraphicalUIinit ()
		{
			Message.KeyBuffer = new byte[256];
			Message.KeyBufferMask = new byte[256];
			Parent = new Form ();
			Parent.Focus (null, null);

			MousePointGettingMethod = delegate {
				return new pointint (0, 0);
			};
			MouseClickGettingMethod = delegate {
				return 0;
			};
			MouseWheelGettingMethod = delegate {
				return 0;
			};
			KeyboardGettingMethod = delegate {
				return new byte[256];
			};
			KeyboardStateGettingMethod = delegate {
				return false;
			};
			JoypadStateGettingMethod = delegate {
				return 0;
			};

		}

		public static void SetTitleBarFont (int FHandle)
		{
			TitleBarFont = FHandle;
		}

		public static void SetBaseFont (int FHandle)
		{
			BaseFont = FHandle;
		}

		public static void SetButtonFont (int FHandle)
		{
			ButtonFont = FHandle;
		}

		//ControlsにFormを追加する。zソートは今はしない
		public static int Add (Form form)
		{
			Controls.Add (form);
			form.SetParenet (Parent);
			return 0;
		}

		public static bool Remove (Form form)
		{
			return Controls.Remove (form);
		}

		//アクティブなFormを最前面に持っていく
		public static int SortZBuffer ()
		{
			Form Buffer;
			for (int i = 0; i < Controls.Count; i++) {
				if (Controls [i].Focused) {
					Buffer = Controls [i];
					Controls.RemoveAt (i);
					Controls.Add (Buffer);
					break;
				}
			}
			return 0;
		}

		public static int SetMousePointGettingMethod (Func<pointint> method)
		{
			MousePointGettingMethod = method;
			return 0;
		}

		public static int SetMouseClickGettingMethod (Func<int> method)
		{
			MouseClickGettingMethod = method;
			return 0;
		}

		public static int SetMouseWheelGettingMethod (Func<int> method)
		{
			MouseWheelGettingMethod = method;
			return 0;
		}

		public static int SetKeyboardGettingMethod (Func<byte[]> method)
		{
			KeyboardGettingMethod = method;
			return 0;
		}

		public static int SetKeyboardStateGettingMethod (Func<int,bool> method)
		{
			KeyboardStateGettingMethod = method;
			return 0;
		}

		public static int Routine ()
		{
			//1.Making Message
			GUIMessage Msg = new GUIMessage ();
			//Collect inputs
			Msg.MousePoint = MousePointGettingMethod.Invoke ();
			Msg.MouseClickState = MouseClickGettingMethod.Invoke ();
			Msg.MouseWheelInput = MouseWheelGettingMethod ();
			Msg.KeyBuffer = KeyboardGettingMethod ();
			Msg.KeyBufferMask = Message.KeyBuffer.Clone () as byte[];
			//CheckDifferences
			//Mouse
			//Cursor
			if (!pointint.Equals (Msg.MousePoint, Message.MousePoint)) {
				Msg.AddMessage (Message_Cursor_Move);
				Msg.MouseMove = Msg.MousePoint - Message.MousePoint;
			}
			//Wheel
			if (Msg.MouseWheelInput != 0) {
				Msg.AddMessage (Message_MouseWheel_Role);
			}
			//Button
			if ((Msg.MouseClickState & DX.MOUSE_INPUT_LEFT) != 0) {
				Msg.AddMessage (Message_Mouse_LeftClick);
				//クリック判定(長押しでない)
				if (Message.MouseClickState != Msg.MouseClickState) {
					Msg.AddMessage (Message_Mouse_Click);
				}
			}
			if ((Msg.MouseClickState & DX.MOUSE_INPUT_RIGHT) != 0) {
				Msg.AddMessage (Message_Mouse_RightClick);
				//クリック判定(長押しでない)
				if (Message.MouseClickState != Msg.MouseClickState) {
					Msg.AddMessage (Message_Mouse_Click);
				}
			}
			if ((Msg.MouseClickState & DX.MOUSE_INPUT_MIDDLE) != 0) {
				Msg.AddMessage (Message_Mouse_MiddleClick);
				//クリック判定(長押しでない)
				if (Message.MouseClickState != Msg.MouseClickState) {
					Msg.AddMessage (Message_Mouse_Click);
				}
			}
			//Wheel
			if (Msg.MouseWheelInput != 0) {
				Msg.AddMessage (Message_MouseWheel_Role);
			}

			//Keyboard
			//省略

			//2.Sending Message
			Parent.Focus (null, null);
			for (int i = Controls.Count - 1; i >= 0; i--) {
				Controls [i].SendMessage (Msg);
				Controls [i].ProcessMessage ();
				if (Parent.Focused && Controls [i].Focused) {
					Parent.Unfocus (null, null);
				}
			}

			//Zソート
			if (Message.Message != 0) {
				SortZBuffer ();
			}

			//3.Copy message to buffer
			Message.Message = Msg.Message;
			Message.MouseClickState = Msg.MouseClickState;
			Message.MousePoint = Msg.MousePoint;
			Msg.KeyBuffer.CopyTo (Message.KeyBuffer, 0);
			Message.MouseWheelInput = Msg.MouseWheelInput;

			//4.描画命令(Controlsはzソートされているのでそのまま描画できる)
			foreach (Control alge in Controls) {
				alge.Render ();
			}
			return 0;
		}
	}

	public class Control : ICloneable
	{
		public virtual pointint Position { get; set; }

		public virtual pointint Size { get; set; }

		protected List<Control> Controls = new List<Control> ();
		protected Control ParentControl;
		protected string Label;
		protected GUIMessage Message = new GUIMessage ();

		public const int MoveControl_Mode_Relative = 0;
		public const int MoveControl_Mode_Absolute = 1;

		protected Dictionary<string,int> Dict;

		//Skins
		protected uint TextColor = DX.GetColor (0, 0, 0);
		public virtual int BaseMargin { get; set; } = 4;
		//周りのマージ(px)

		public int SkinTop;
		public int SkinBottom;
		public int SkinRight;
		public int SkinLeft;
		public int SkinTopLeft;
		public int SkinTopRight;
		public int SkinBottomLeft;
		public int SkinBottomRight;
		public int SkinBase;

		//Events
		public event EventHandler Click;
		public event EventHandler GotFocus;
		public event EventHandler LostFocus;
		public event EventHandler MouseHover;
		public event EventHandler MouseLeave;
		public event EventHandler MouseMove;
		public event EventHandler MouseWheel;
		public event EventHandler Move;
		public event EventHandler Paint;
		//And more...

		public virtual void OnClick (EventArgs args)
		{
			var handler = this.Click;
			if (handler != null)
				handler (this, args);
		}


		public virtual void OnGotFocus (EventArgs args)
		{
			var handler = this.GotFocus;
			if (handler != null)
				handler (this, args);
		}


		public virtual void OnLostFocus (EventArgs args)
		{
			var handler = this.LostFocus;
			if (handler != null)
				handler (this, args);
		}


		public virtual void OnMouseHover (EventArgs args)
		{
			var handler = this.MouseHover;
			if (handler != null)
				handler (this, args);
		}


		public virtual void OnMouseLeave (EventArgs args)
		{
			var handler = this.MouseLeave;
			if (handler != null)
				handler (this, args);
		}


		public virtual void OnMouseMove (EventArgs args)
		{
			var handler = this.MouseMove;
			if (handler != null)
				handler (this, args);
		}


		public virtual void OnMouseWheel (EventArgs args)
		{
			var handler = this.MouseWheel;
			if (handler != null)
				handler (this, args);
		}


		public virtual void OnMove (EventArgs args)
		{
			var handler = this.Move;
			if (handler != null)
				handler (this, args);
		}


		public virtual void OnPaint (EventArgs args)
		{
			var handler = this.Paint;
			if (handler != null)
				handler (this, args);
		}
		
		//Properties
		public virtual pointint Location {
			get {
				return Position - ParentControl.Position;
			}

			set {
				pointint PosBuf = Position;
				Position = ParentControl.Position + value;
				for (int i = 0; i < Controls.Count; i++) {
					Controls [i].Location = new pointint (Controls [i].Position.x - PosBuf.x, Controls [i].Position.y - PosBuf.y);
				}
			}
		}

		public virtual bool Focused { get; private set; } = false;

		//カーソルがControlの上に乗っているときにtrue
		public virtual bool Pointed { get; private set; } = false;

		public virtual string Text { get; set; } = "";


		/// <summary>
		/// Clone this instance. Events are reset as default.
		/// </summary>
		public virtual object Clone()
		{
			Control instance = (Control)this.MemberwiseClone ();
			instance.GotFocus = instance.Focus;
			instance.LostFocus = instance.Unfocus;
			instance.Move = instance.Control_Move;
			instance.Click      = null;
			instance.MouseHover = null;
			instance.MouseLeave = null;
			instance.MouseMove  = null;
			instance.MouseWheel = null;
			instance.Paint      = null;
			instance.Message = new GUIMessage ();
			instance.Controls = new List<Control> (this.Controls);
			return instance;
		}


		public Control ()
		{
			//Focusをイベントに関連付ける
			GotFocus += Focus;
			LostFocus += Unfocus;

			//コントロールの移動イベント(自身が移動するためのイベント)
			Move += Control_Move;
		}

		public void InitializeDictionary ()
		{
			Dict = new Dictionary<string, int> ();
		}

		public void InitializeDictionary (int Capacity)
		{
			Dict = new Dictionary<string, int> (Capacity);
		}

		public void AddDictionary (string Key, int Value)
		{
			if (Dict != null)
				Dict.Add (Key, Value);
		}

		void Control_Move (Control sender, EventArgs args)
		{
			sender.MoveControl (this.Message.MouseMove, Control.MoveControl_Mode_Relative);
			for (int i = 0; i < Controls.Count; i++) {
				Controls [i].OnMove (null);
			}
			return;
		}

		public void SetParenet (Control parent)
		{
			ParentControl = parent;
			return;
		}

		public Control GetParent ()
		{
			return ParentControl;
		}

		/// <summary>
		/// Null例外を避けるイベントInvokeメソッドです.イベントはこれを経由してInvokeすることを推奨します.
		/// </summary>
		/// <param name="e">イベント</param>
		/// <param name="sender">呼び出し主</param>
		/// <param name="args">引数</param>
		public void InvokeEvent (EventHandler e, Control sender, EventArgs args)
		{
			e?.Invoke (sender, args);
			return;
		}

		public virtual void AddChild (Control control)
		{
			Controls.Add (control);
			control.SetParenet (this);
			return;
		}

		public virtual void RemoveChild (Control control)
		{
			for (int i = 0; i < Controls.Count; i++) {
				if (Controls [i] == control) {
					Controls.RemoveAt (i);
					break;
				}
			}
			return;
		}

		public virtual void SendMessage (GUIMessage message)
		{
			message.CopyTo (out Message);
			//Controlsにもメッセージを送ります
			foreach (Control alge in Controls) {
				alge.SendMessage (message);
			}
		}

		public virtual void SetBounds (int x, int y, int width, int height)
		{
			Position = new pointint (x, y);
			Size = new pointint (width, height);
			return;
		}

		/// <summary>
		/// スキン用に画像を切り抜き設定します。
		/// </summary>
		/// <param name="GHandle">グラフィックハンドル(DxLib)</param>
		public virtual void SetSkin (int GHandle)
		{
			pointint GSize;
			DX.GetGraphSize (GHandle, out GSize.x, out GSize.y);
			SkinTop = DX.DerivationGraph (BaseMargin, 0, GSize.x - (2 * BaseMargin), BaseMargin, GHandle);
			SkinBottom = DX.DerivationGraph (BaseMargin, GSize.y - BaseMargin, GSize.x - (2 * BaseMargin), BaseMargin, GHandle);
			SkinLeft = DX.DerivationGraph (0, BaseMargin, BaseMargin, GSize.y - (2 * BaseMargin), GHandle);
			SkinRight = DX.DerivationGraph (GSize.x - BaseMargin, BaseMargin, BaseMargin, GSize.y - (2 * BaseMargin), GHandle);
			SkinTopLeft = DX.DerivationGraph (0, 0, BaseMargin, BaseMargin, GHandle);
			SkinTopRight = DX.DerivationGraph (GSize.x - BaseMargin, 0, BaseMargin, BaseMargin, GHandle);
			SkinBottomLeft = DX.DerivationGraph (0, GSize.y - BaseMargin, BaseMargin, BaseMargin, GHandle);
			SkinBottomRight = DX.DerivationGraph (GSize.x - BaseMargin, GSize.y - BaseMargin, BaseMargin, BaseMargin, GHandle);
			SkinBase = DX.DerivationGraph (BaseMargin, BaseMargin, GSize.x - (2 * BaseMargin), GSize.y - (2 * BaseMargin), GHandle);
			return;
		}

		public virtual void SetSkin (string GPath)
		{
			SetSkin (DX.LoadGraph (GPath));
			return;
		}

		public virtual int Render ()
		{
			//ベースを描く
			DX.DrawExtendGraph (Position.x + BaseMargin, Position.y + BaseMargin, Position.x + Size.x - BaseMargin, Position.y + Size.y - BaseMargin, SkinBase, DX.TRUE);
			//枠を描く
			DX.DrawExtendGraph (Position.x, Position.y + BaseMargin, Position.x + BaseMargin, Position.y + Size.y - BaseMargin, SkinLeft, DX.TRUE);
			DX.DrawExtendGraph (Position.x + Size.x - BaseMargin, Position.y + BaseMargin, Position.x + Size.x, Position.y + Size.y - BaseMargin, SkinRight, DX.TRUE);
			DX.DrawExtendGraph (Position.x + BaseMargin, Position.y, Position.x + Size.x - BaseMargin, Position.y + BaseMargin, SkinTop, DX.TRUE);
			DX.DrawExtendGraph (Position.x + BaseMargin, Position.y + Size.y - BaseMargin, Position.x + Size.x - BaseMargin, Position.y + Size.y, SkinBottom, DX.TRUE);
			DX.DrawGraph (Position.x, Position.y, SkinTopLeft, DX.TRUE);
			DX.DrawGraph (Position.x, Position.y + Size.y - BaseMargin, SkinBottomLeft, DX.TRUE);
			DX.DrawGraph (Position.x + Size.x - BaseMargin, Position.y, SkinTopRight, DX.TRUE);
			DX.DrawGraph (Position.x + Size.x - BaseMargin, Position.y + Size.y - BaseMargin, SkinBottomRight, DX.TRUE);
			return 0;
		}

		public virtual int ProcessMessage ()
		{
			//メッセージ処理をここに記述します。イベントの発生もここで行います。
			//Message_Mouse_Click
			if ((Message.Message & GraphicalUI.Message_Mouse_Click) != 0) {
				if (Pointed) {
					if (!Focused) {
						if (ParentControl.Focused) {
							InvokeEvent (GotFocus, this, null);
						}
					}
					InvokeEvent (Click, this, null);
				} else if (Focused) {
					InvokeEvent (LostFocus, this, null);
				}
			}
			//Message_Cursor_Move
			if ((Message.Message & GraphicalUI.Message_Cursor_Move) != 0) {
				if ((Message.MousePoint.x >= Position.x) && (Message.MousePoint.x <= Position.x + Size.x) &&
				    (Message.MousePoint.y >= Position.y) && (Message.MousePoint.y <= Position.y + Size.y)) {
					if (!Pointed) {
						Pointed = true;
					}
				} else {
					if (Pointed) {
						Pointed = false;
						InvokeEvent (MouseLeave, this, null);
					}
				}
			}
			//Mouse Hover
			if (Pointed) {
				InvokeEvent (MouseHover, this, null);
			}

			return 0;
		}

		/// <summary>
		/// コントロールを移動します.
		/// </summary>
		/// <param name="amout">移動先座標or移動量.</param>
		/// <param name="mode">0 = 移動先座標, 1 = 移動量</param>
		public virtual void MoveControl (pointint amount, int mode)
		{
			if (mode == MoveControl_Mode_Absolute) {
				Position = amount;
			} else if (mode == MoveControl_Mode_Relative) {
				Position += amount;
			}
			return;
		}


		public virtual void Focus (Control sender, EventArgs args)
		{
			Focused = true;
			return;
		}

		public virtual void Unfocus (Control sender, EventArgs args)
		{
			Focused = false;
			return;
		}


		public Control GetControl (int index)
		{
			if (index == -1)
				return this;

			if (index >= Controls.Count)
				return null;

			if (index < 0)
				return null;

			return Controls [index];
		}

		public Control GetControl (string id)
		{
			if (Dict == null)
				return null;

			return GetControl (Dict [id]);
		}

		public Control this [int index] {
			get {
				return GetControl (index);
			}
			set {
				throw new NotImplementedException ();
			}
		}

		public Control this [string index] {
			get {
				return GetControl (index);
			}
			set {
				throw new NotImplementedException ();
			}
		}
	}

	public class Form : Control
	{
		public const int TitleBarButton_Close = 1;
		public const int TitleBarButton_MaxMinimize = 2;
		public const int TitleBarButton_Store = 4;

		protected rectint TitleBarRect;

		public override pointint Size {
			set {
				base.Size = value;
				TitleBarRect.BottomRight.x = Position.x + value.x;
			}
		}

		public override pointint Position {
			set {
				TitleBarRect.TopLeft = value;
				TitleBarRect.BottomRight = (TitleBarRect.BottomRight - base.Position) + value;
				base.Position = value;
			}
		}

		protected int _TitleBarHeight = 16;

		public int TitleBarHeight {
			get {
				return _TitleBarHeight;
			}
			set {
				_TitleBarHeight = value;
				TitleBarRect.BottomRight.y = Position.y + TitleBarHeight;
			}
		}

		public int TitleBarMargin { get; set; } = 4;

		public int TitleBarStoreButtonOffset { get; set; } = 48;

		public int TitleBarMaxMinimizeButtonOffset { get; set; } = 32;

		public int TitleBarCloseButtonOffset { get; set; } = 16;

		protected Button CloseButton;
		protected Button MaxMinimizeButton;
		protected Button StoreButton;

		//Skins

		public int SkinClose;
		public int SkinMaximize;
		public int SkinMinimize;
		public int SkinStore;
		public int SkinTitleBarBase;
		public int SkinTitleBarRight;
		public int SkinTitleBarLeft;

		//Design
		protected pointint TitlebarTextOfset;

		//Properties
		public bool TitleBarGrabbed { get; private set; }

		//Events
		protected event EventHandler TitleBarGrab;

		public Form () : this (0, 0, 300, 300, "", 0)
		{
			//Default setting of Form.
		}

		public Form (int px, int py, int width, int height, string label, int TitleBarButtonType)
		{
			Position = new pointint (px, py);
			Size = new pointint (width, height); 
			Text = label;
			TitleBarRect.TopLeft.x = Position.x;
			TitleBarRect.TopLeft.y = Position.y;
			TitleBarRect.BottomRight.x = Position.x + Size.x;
			TitleBarRect.BottomRight.y = Position.y + TitleBarHeight;
			TitlebarTextOfset.y = TitleBarMargin;


			int ButtonBasePos = Size.x - TitleBarMargin;
			if ((TitleBarButtonType & TitleBarButton_Close) != 0) {
				CloseButton = new Button (this, ButtonBasePos - TitleBarCloseButtonOffset, 0, 16, 16);
				AddChild (CloseButton);
			}
			if ((TitleBarButtonType & TitleBarButton_MaxMinimize) != 0) {
				MaxMinimizeButton = new Button (this, ButtonBasePos - TitleBarMaxMinimizeButtonOffset, 0, 16, 16);
				AddChild (MaxMinimizeButton);
			}
			if ((TitleBarButtonType & TitleBarButton_Store) != 0) {
				StoreButton = new Button (this, ButtonBasePos - TitleBarStoreButtonOffset, 0, 16, 16);
				AddChild (StoreButton);
			}

			//Add Default Events
			TitleBarGrab += Form_TitleBarGrab;
		}

		void Form_TitleBarGrab (Control sender, EventArgs args)
		{
			//Console.WriteLine ("grabbed");
		}

		public override int ProcessMessage ()
		{
			base.ProcessMessage ();
			//タイトルバーをつかむ
			if (Focused && TitleBarRect.Includes (Message.MousePoint) && ((Message.Message & GraphicalUI.Message_Mouse_LeftClick) != 0)) {
				TitleBarGrabbed = true;
			}
			if (TitleBarGrabbed && ((Message.Message & GraphicalUI.Message_Mouse_LeftClick) == 0)) {
				TitleBarGrabbed = false;
			}
			if (TitleBarGrabbed) {
				InvokeEvent (TitleBarGrab, this, null);
			}

			//Window 移動
			if (TitleBarGrabbed && ((Message.Message & GraphicalUI.Message_Cursor_Move) != 0)) {
				OnMove (null);
			}

			//子コントロールのメッセージ処理
			for (int i = 0; i < Controls.Count; i++) {

				Controls [i].ProcessMessage ();
			}

			return 0;
		}

		public override void SetSkin (int GHandle)
		{
			pointint GSize;
			DX.GetGraphSize (GHandle, out GSize.x, out GSize.y);
			SkinTop = DX.DerivationGraph (TitleBarMargin, 0, GSize.x - 2 * TitleBarMargin, TitleBarMargin, GHandle);
			SkinBottom = DX.DerivationGraph (BaseMargin, GSize.y - BaseMargin, GSize.x - 2 * BaseMargin, BaseMargin, GHandle);
			SkinLeft = DX.DerivationGraph (0, TitleBarHeight, BaseMargin, GSize.y - BaseMargin - TitleBarHeight, GHandle);
			SkinRight = DX.DerivationGraph (GSize.x - BaseMargin, TitleBarHeight, BaseMargin, GSize.y - BaseMargin - TitleBarHeight, GHandle);
			SkinTitleBarLeft = DX.DerivationGraph (0, 0, TitleBarMargin, TitleBarHeight, GHandle);
			SkinTitleBarRight = DX.DerivationGraph (GSize.x - TitleBarMargin, 0, TitleBarMargin, TitleBarHeight, GHandle);
			SkinTitleBarBase = DX.DerivationGraph (TitleBarMargin, 0, GSize.x - 2 * TitleBarMargin, TitleBarHeight, GHandle);
			SkinBottomLeft = DX.DerivationGraph (0, GSize.y - BaseMargin, BaseMargin, BaseMargin, GHandle);
			SkinBottomRight = DX.DerivationGraph (GSize.x - BaseMargin, GSize.y - BaseMargin, BaseMargin, BaseMargin, GHandle);
			SkinBase = DX.DerivationGraph (BaseMargin, TitleBarHeight, GSize.x - 2 * BaseMargin, GSize.y - BaseMargin - TitleBarHeight, GHandle);
			return;
		}

		public override void SetSkin (string GPath)
		{
			SetSkin (DX.LoadGraph (GPath));
			return;
		}

		public override int Render ()
		{
			//TitleBar
			DX.DrawGraph (Position.x, Position.y, SkinTitleBarLeft, DX.TRUE);
			DX.DrawGraph (Position.x + Size.x - TitleBarMargin, Position.y, SkinTitleBarRight, DX.TRUE);
			DX.DrawExtendGraph (Position.x + TitleBarMargin, Position.y, Position.x + Size.x - TitleBarMargin, Position.y + TitleBarHeight, SkinTitleBarBase, DX.TRUE);
			//Base
			DX.DrawExtendGraph (Position.x, Position.y + TitleBarHeight, Position.x + BaseMargin, Position.y + Size.y - BaseMargin, SkinLeft, DX.TRUE);
			DX.DrawExtendGraph (Position.x + Size.x - BaseMargin, Position.y + TitleBarHeight, Position.x + Size.x, Position.y + Size.y - BaseMargin, SkinRight, DX.TRUE);
			DX.DrawExtendGraph (Position.x + BaseMargin, Position.y + Size.y - BaseMargin, Position.x + Size.x - BaseMargin, Position.y + Size.y, SkinBottom, DX.TRUE);
			DX.DrawGraph (Position.x, Position.y + Size.y - BaseMargin, SkinBottomLeft, DX.TRUE);
			DX.DrawGraph (Position.x + Size.x - BaseMargin, Position.y + Size.y - BaseMargin, SkinBottomRight, DX.TRUE);
			DX.DrawExtendGraph (Position.x + BaseMargin, Position.y + TitleBarHeight, Position.x + Size.x - BaseMargin, Position.y + Size.y - BaseMargin, SkinBase, DX.TRUE);

			DX.DrawStringToHandle (Position.x + TitleBarMargin + TitlebarTextOfset.x, Position.y + TitlebarTextOfset.y, Text, DX.GetColor (0, 0, 0), GraphicalUI.TitleBarFont);
			foreach (var alge in Controls) {
				alge.Render ();
			}
			return 0;
		}

		public override void MoveControl (pointint amount, int mode)
		{
			if (mode == Control.MoveControl_Mode_Absolute) {
				Position = amount;
			} else if (mode == Control.MoveControl_Mode_Relative) {
				base.Position += amount;
				TitleBarRect.TopLeft += amount;
				TitleBarRect.BottomRight += amount;
			}
			return;
		}
	}

	public class Button : Control
	{
		private int StrWidth;
		private int StrHeight;
		private pointint TextUpLeft;

		public override string Text {
			set {
				base.Text = value;
				StrWidth = DX.GetDrawStringWidthToHandle (Text, Text.Length, GraphicalUI.ButtonFont);
				TextUpLeft.x = (Size.x - StrWidth) / 2;
				TextUpLeft.y = (Size.y - StrHeight) / 2;
			}
		}

		public Button () : this (0, 0, 200, 50)
		{
			Position = new pointint (0, 0);
			Size = new pointint (200, 50);

			//Events

		}

		public Button (int px, int py, int width, int height)
		{
			Position = new pointint (px, py);
			Size = new pointint (width, height);
		}

		public Button (Form Parent, int lx, int ly, int width, int height)
		{
			Position = new pointint (Parent.Position.x + lx, Parent.Position.y + ly);
			Size = new pointint (width, height);
		}

		public override int Render ()
		{
			base.Render ();
			DX.DrawStringToHandle (Position.x + TextUpLeft.x, Position.y + TextUpLeft.y, Text, TextColor, GraphicalUI.ButtonFont);
			return 0;
		}
	}

	public class TextBox : Control
	{
		private pointint TextUpLeft;
		private int InputHandle = -1;
		private uint BufferSize;
		private int SingleCharOnlyFlag = DX.FALSE;
		private int NumberCharOnlyFlag = DX.FALSE;
		private System.Text.StringBuilder Buffer;

		public TextBox ()
		{
			BufferSize = 256;
			Position = new pointint (0, 0);
			Size = new pointint (200, 20);
			Text = "Input Await";
			GotFocus += InputAwait;
			LostFocus += InputUnawait;
		}

		private void InputAwait (Control sender, EventArgs args)
		{
			if (InputHandle == -1) {
				InputHandle = DX.MakeKeyInput (BufferSize, DX.FALSE, SingleCharOnlyFlag, NumberCharOnlyFlag);
			}
			DX.SetActiveKeyInput (InputHandle);
			DX.SetKeyInputString (Text, InputHandle);
			return;
		}

		private void InputUnawait (Control sender, EventArgs args)
		{
			Text = GetBuffer ();
			DX.DeleteKeyInput (InputHandle);
			InputHandle = -1;
			return;
		}

		private string GetBuffer ()
		{
			if (InputHandle == -1) {
				return Text;
			} else {
				if (Buffer == null) {
					Buffer = new System.Text.StringBuilder ((int)BufferSize);
				}
				DX.GetKeyInputString (Buffer, InputHandle);
			}
			return Buffer.ToString ();
		}

		public override int Render ()
		{
			Text = GetBuffer ();
			base.Render ();
			DX.DrawStringToHandle (Position.x + TextUpLeft.x, Position.y + TextUpLeft.y, Text, TextColor, GraphicalUI.BaseFont);
			foreach (var alge in Controls) {
				alge.Render ();
			}
			return 0;
		}
	}

	public class Label : Control
	{
		protected string[] WrappedText;
		protected int WrapLength;

		public int TextHeight { get; set; } = 20;

		public Label (int px, int py, int width, int height, string text)
		{
			Position = new pointint (px, py);
			Size = new pointint (width, height);
			Text = text;
		}

		public Label (int width, int height, string text)
		{
			Size = new pointint (width, height);
			Text = text;
		}

		public override string Text {
			set {
				base.Text = value;
				WrapText ();
			}
		}

		public void WrapText ()
		{
			if (Text.Length == 0)
				return;
			double CharWidth = (double)DX.GetDrawStringWidthToHandle (Text, Text.Length, GraphicalUI.BaseFont) / Text.Length;
			if (CharWidth < 0)
				return;
			WrapLength = (int)(Size.x / CharWidth);
			WrappedText = DXEx.GenerateWrapString (WrapLength, Text);
		}

		public override int Render ()
		{
			if (WrappedText == null) {
				return -1;
			}

			int YOfset = 0;
			for (int i = 0; i < WrappedText.Length; i++) {
				if (YOfset + TextHeight > Size.y) {
					break;
				}
				DX.DrawStringToHandle (Position.x, Position.y + YOfset, WrappedText [i], TextColor, GraphicalUI.BaseFont);
				YOfset += TextHeight;
			}
			return 0;
		}
	}

	public class ScrollBar : Control
	{
		public int ScrollBarMargin { get; set; } = 16;

		public int ButtonHeight { get; set; } = 16;


		public int SkinTopButton;
		public int SkinBottomButton;

		private int _Value;

		public int Value {
			get {
				return _Value;
			}
			set {
				if (value > Max)
					_Value = Max;
				else if (value < Min)
					_Value = Min;
				else
					_Value = value;
			}
		}

		public int Max, Min;

		public Button TopButton;
		public Button BottomButton;

		public ScrollBar (int px, int py, int Min, int Max, int DefaultVal)
		{
			Position = new pointint (px, py);
			this.Max = Max;
			this.Min = Min;
			Value = DefaultVal;

			TopButton = new Button ();
			BottomButton = new Button ();
			AddChild (TopButton);
			AddChild (BottomButton);

			TopButton.BaseMargin = 0;
			BottomButton.BaseMargin = 0;

			TopButton.Click += TopButton_Click;
			BottomButton.Click += BottomButton_Click;
		}

		void BottomButton_Click (Control sender, EventArgs args)
		{
			SubValue (10);
		}

		void TopButton_Click (Control sender, EventArgs args)
		{
			AddValue (10);
		}

		private int AddValue (int val)
		{
			Value += val;
			return 0;
		}

		private int SubValue (int val)
		{
			Value -= val;
			return 0;
		}
	}

	public class HScrollBar : ScrollBar
	{
		public HScrollBar (int px, int py, int width, int Min, int Max, int DefaultValue) : base (px, py, Min, Max, DefaultValue)
		{
		}
	}

	public class VScrollBar : ScrollBar
	{
		public VScrollBar (int px, int py, int height, int Min, int Max, int DefaultValue) : base (px, py, Min, Max, DefaultValue)
		{
			Size = new pointint (0, height);
			TopButton.Location = new pointint (0, 0);
			BottomButton.Location = new pointint (0, Size.y - ButtonHeight);
		}

		public override void SetSkin (int GHandle)
		{
			pointint GSize;
			DX.GetGraphSize (GHandle, out GSize.x, out GSize.y);
			SkinTop = DX.DerivationGraph (0, ButtonHeight, GSize.x, ScrollBarMargin, GHandle);
			SkinBottom = DX.DerivationGraph (0, GSize.y - ButtonHeight - ScrollBarMargin, GSize.x, ScrollBarMargin, GHandle);
			SkinBase = DX.DerivationGraph (0, ScrollBarMargin + ButtonHeight, GSize.x, GSize.y - 2 * (ButtonHeight + ScrollBarMargin), GHandle);
			SkinTopButton = DX.DerivationGraph (0, 0, GSize.x, ButtonHeight, GHandle);
			SkinBottomButton = DX.DerivationGraph (0, GSize.y - ButtonHeight, GSize.x, ButtonHeight, GHandle);
			TopButton.SetSkin (SkinTopButton);
			BottomButton.SetSkin (SkinBottomButton);
			TopButton.Size = new pointint (GSize.x, ButtonHeight);
			BottomButton.Size = new pointint (GSize.x, ButtonHeight);

			Size = new pointint (GSize.x, Size.y);

			return;
		}

		public override int Render ()
		{
			DX.DrawGraph (Position.x, Position.y + ButtonHeight, SkinTop, DX.TRUE);
			DX.DrawGraph (Position.x, Position.y + Size.y - ButtonHeight - ScrollBarMargin, SkinBottom, DX.TRUE);
			DX.DrawExtendGraph (Position.x, Position.y + ButtonHeight + ScrollBarMargin, Position.x + Size.x, Position.y + Size.y - ButtonHeight - ScrollBarMargin,
				SkinBase, DX.TRUE);
			
			foreach (var alge in Controls) {
				alge.Render ();
			}
			return 0;
		}
	}

	public class ListControl<Type> : Control
	{
		public int CaptionMargin { get; set; } = 20;

		public int ThumbnailMargin { get; set; } = 4;

		public int ItemHeight { get; set; } = 20;

		public class ListItem
		{
			public int Thumbnail;
			public string Caption;
			public Type Item;

			public ListItem (Type item, string caption, int thumbnail)
			{
				Item = item;
				Caption = caption;
				Thumbnail = thumbnail;
			}
		}

		protected List<ListItem> Data;
		protected List<ListItem> FoundData;
		protected int Selection;
		protected int ListImage = -1;

		public override object Clone ()
		{
			ListControl<Type> instance = (ListControl<Type>)base.Clone ();
			instance.Data = new List<ListItem> (this.Data);
			return instance;
		}

		public ListControl () : this (15)
		{
		}

		public ListControl (int Capacity)
		{
			Data = new List<ListItem> (Capacity);
			Selection = 0;
			Size = new pointint (100, 100);
			AddChild (new VScrollBar (0, 0, this.Size.y, 0, 100, 0));
			Controls [0].Location = new pointint (Size.x - Controls [0].Size.x, 0);

			Click += ListControl_Click;
		}

		void ListControl_Click (Control sender, EventArgs args)
		{
			int index;
			rectint ListSize = new rectint (Position.x, Position.y, Position.x + Size.x - Controls [0].Size.x, Position.y + Size.y);
			if (ListSize.Includes (Message.MousePoint)) {
				index = (int)(((Controls [0] as VScrollBar).Value + (Message.MousePoint.y - Position.y)) / ItemHeight);
				if (index > Data.Count - 1) {
					return;
				}
				Selection = index;
			}
			return;	
		}

		public void Add (Type item, string caption, int thumbnail)
		{
			Data.Add (new ListItem (item, caption, thumbnail));
			RenderList (Data);
			return;
		}

		public void Insert (int index, Type item, string caption, int thumbnail)
		{
			Data.Insert (index, new ListItem (item, caption, thumbnail));
			RenderList (Data);
			return;
		}

		public void FindBegin (Predicate<ListItem> pred)
		{
			FoundData = Data.FindAll (pred);
			RenderList (FoundData);
			return;
		}

		public void FindEnd ()
		{
			RenderList (Data);
		}

		public override int ProcessMessage ()
		{
			base.ProcessMessage ();

			//Controls[0]
			if (Pointed && ((Message.Message & GraphicalUI.Message_MouseWheel_Role) != 0)) {
				(Controls [0] as VScrollBar).Value -= Message.MouseWheelInput * 10;
			}

			for (int i = 0; i < Controls.Count; i++) {
				Controls [i].ProcessMessage ();
			}
			return 0;
		}

		public void RenderList (List<ListItem> source)
		{
			if (ListImage != -1) {
				DX.DeleteGraph (ListImage);
				ListImage = -1;
			}
			ListImage = DX.MakeScreen (Size.x, ItemHeight * Data.Count);
			(Controls [0] as VScrollBar).Max = (ItemHeight * Data.Count) - this.Size.y;
			int YOffset = 0;
			int PrevScreen = DX.GetDrawScreen ();
			DX.SetDrawScreen (ListImage);
			foreach (var alge in source) {
				DX.DrawExtendGraph (0, YOffset, Size.x, ItemHeight + YOffset, SkinBase, DX.TRUE);
				DX.DrawGraph (ThumbnailMargin, YOffset, alge.Thumbnail, DX.TRUE);
				DX.DrawStringToHandle (CaptionMargin, YOffset + ItemHeight / 3, alge.Caption, TextColor, GraphicalUI.BaseFont);
				YOffset += ItemHeight;
			}
			DX.SetDrawScreen (PrevScreen);
			return;
		}

		public override int Render ()
		{
			
			DX.DrawRectGraph (Position.x, Position.y, 0, (Controls [0] as VScrollBar).Value, Size.x, Size.y, ListImage, DX.TRUE, DX.FALSE);
			//枠を描く
			DX.DrawExtendGraph (Position.x, Position.y + BaseMargin, Position.x + BaseMargin, Position.y + Size.y - BaseMargin, SkinLeft, DX.TRUE);
			DX.DrawExtendGraph (Position.x + Size.x - BaseMargin, Position.y + BaseMargin, Position.x + Size.x, Position.y + Size.y - BaseMargin, SkinRight, DX.TRUE);
			DX.DrawExtendGraph (Position.x + BaseMargin, Position.y, Position.x + Size.x - BaseMargin, Position.y + BaseMargin, SkinTop, DX.TRUE);
			DX.DrawExtendGraph (Position.x + BaseMargin, Position.y + Size.y - BaseMargin, Position.x + Size.x - BaseMargin, Position.y + Size.y, SkinBottom, DX.TRUE);
			DX.DrawGraph (Position.x, Position.y, SkinTopLeft, DX.TRUE);
			DX.DrawGraph (Position.x, Position.y + Size.y - BaseMargin, SkinBottomLeft, DX.TRUE);
			DX.DrawGraph (Position.x + Size.x - BaseMargin, Position.y, SkinTopRight, DX.TRUE);
			DX.DrawGraph (Position.x + Size.x - BaseMargin, Position.y + Size.y - BaseMargin, SkinBottomRight, DX.TRUE);
			Controls [0].Render ();
			return 0;
		}
	}

	public class MessageBox : Form
	{
		private Button ResponceButton;
		private Label MessageLabel;

		public MessageBox (string Message) : base (0, 0, 250, 200, "Message", 0)
		{
			ResponceButton = new Button ();
			MessageLabel = new Label (200, 150, Message);

			AddChild (ResponceButton);
			AddChild (MessageLabel);

			ResponceButton.SetBounds (15, 160, 100, 25);
			ResponceButton.Text = "Close";
			MessageLabel.Location = new pointint (25, 25);

			ResponceButton.Click += ResponceButton_Click;
		}

		void ResponceButton_Click (Control sender, EventArgs args)
		{
		}

		public override int Render ()
		{
			base.Render ();
			return 0;
		}

		public override int ProcessMessage ()
		{
			return base.ProcessMessage ();
		}
	}

	public class XmlGUICreator : Form
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TmlGUI.XmlGUICreator"/> class. You shold use Create() instead.
		/// </summary>
		/// <param name="XmlPath">Xml path.</param>
		public XmlGUICreator (string XmlPath)
		{
			Dict = new Dictionary<string, int> ();
			Control Buffer = null;
			Control Current = this;
			Stack<Control> controlS = new Stack<Control> ();
			Stack<int> indexS = new Stack<int> ();
			int index = 0;
			XmlReaderSettings Setting = new XmlReaderSettings ();
			Setting.IgnoreComments = true;
			using (XmlReader Handle = XmlReader.Create (new StreamReader (XmlPath), Setting)) {
				//デフォルトのFormを読み込む
				if (Handle.MoveToContent () == XmlNodeType.Element && Handle.Name == "Form") {
					if (Handle.HasAttributes) {
						InitializeDictionary ();
						AddDictionary (Handle.GetAttribute ("Id") ?? "BaseForm", -1);

						Position = new pointint (int.Parse (Handle.GetAttribute ("X")), int.Parse (Handle.GetAttribute ("Y")));
						Size = new pointint (int.Parse (Handle.GetAttribute ("Width")), int.Parse (Handle.GetAttribute ("Height")));
						Text = Handle.GetAttribute ("Text") ?? "";
						SetSkin (Handle.GetAttribute ("Skin"));
					}
				}
				Handle.MoveToContent ();
				while (Handle.Read ()) {
					if (Handle.NodeType != XmlNodeType.Element) {
						if (Handle.NodeType != XmlNodeType.EndElement)
							continue;

						if (Handle.Depth == 0)
							break;
						index = indexS.Pop ();
						Current = controlS.Pop ();
						continue;
					}
					//==コントロールを作成(デフォルトのクローン)==
					switch (Handle.Name) {
					/*case "Form":
						Buffer = ResourceManager.GetDefault (typeof(Form));*/
					case "Button":
						Buffer = ResourceManager.GetDefault (typeof(Button));
						Console.WriteLine (Buffer.GetType ().Name);
						break;
					case "Label":
						Buffer = ResourceManager.GetDefault (typeof(Label));
						break;
					case "TextBox":
						Buffer = ResourceManager.GetDefault (typeof(TextBox));
						break;
					case "VScrollBar":
						Buffer = ResourceManager.GetDefault (typeof(VScrollBar));
						break;
					case "ListControl":
						Buffer = ResourceManager.GetDefault (typeof(ListControl<int>));
						break;
					}

					//==コントロールに共通な項目を読み込み==
					//-辞書に登録-
					Current.AddDictionary (Handle.GetAttribute ("Id") ?? "BaseForm", index);
					index++;
					//-サイズなど-
					Buffer.Position = new pointint (Current.Position.x + int.Parse (Handle.GetAttribute ("X")), Current.Position.y + int.Parse (Handle.GetAttribute ("Y")));
					Buffer.Size = new pointint (int.Parse (Handle.GetAttribute ("Width")), int.Parse (Handle.GetAttribute ("Height")));
					Buffer.Text = Handle.GetAttribute ("Text") ?? "";
					Buffer.BaseMargin = Parse (Handle.GetAttribute ("BaseMargin"), Buffer.BaseMargin);

					//==コントロールに特有な値の設定==
					//-マージンの取り込みの改善をすること-
					//TODO Improve how to get margins.
					switch (Handle.Name) {
					case "VScrollBar":
						(Buffer as VScrollBar).ScrollBarMargin = Parse (Handle.GetAttribute ("ScrollBarMargin"), (Buffer as VScrollBar).ScrollBarMargin);
						(Buffer as VScrollBar).ButtonHeight = Parse (Handle.GetAttribute ("ButtonHeight"), (Buffer as VScrollBar).ButtonHeight);
						(Buffer as VScrollBar).Max = Parse (Handle.GetAttribute ("Max"), (Buffer as VScrollBar).Max);
						(Buffer as VScrollBar).Min = Parse (Handle.GetAttribute ("Min"), (Buffer as VScrollBar).Min);
						(Buffer as VScrollBar).Value = Parse (Handle.GetAttribute ("DefaultValue"), (Buffer as VScrollBar).Value);
						break;
					}

					//==スキン==
					//-スキンをセット-
					if (Handle.GetAttribute ("Skin") == "Default" || Handle.GetAttribute("Skin") == null) {
						//何もしない. Nothing to do.
					} else {
						Buffer.SetSkin (Handle.GetAttribute ("Skin"));
					}
					//==コントロールを親に追加==
					if (Buffer != null) {
						Current.AddChild (Buffer);
					}

					//==子コントロールを作るか確認==
					if (!Handle.IsEmptyElement) {
						indexS.Push (index);
						index = 0;
						controlS.Push (Current);
						Current = Buffer;
						Buffer.InitializeDictionary ();
					} else {
						index++;
					}
					Buffer = null;
				}
				Console.WriteLine ("Result of {0}:", XmlPath);
				foreach (var alge in Controls) {
					Console.WriteLine ("{0}.Text = {1}",alge.GetType (),alge.Text);
				}
			}
		}

		public static Form Create (string XmlPath)
		{
			Control Buffer = null;
			Control Current = new Control ();
			Stack<Control> controlS = new Stack<Control> ();
			Stack<int> indexS = new Stack<int> ();
			int index = 0;
			XmlReaderSettings Setting = new XmlReaderSettings ();
			Setting.IgnoreComments = true;
			using (XmlReader Handle = XmlReader.Create (new StreamReader (XmlPath), Setting)) {
				while (Handle.Read ()) {
					if (Handle.NodeType != XmlNodeType.Element) {
						if (Handle.NodeType != XmlNodeType.EndElement)
							continue;

						index = indexS.Pop ();
						Current = controlS.Pop ();
						continue;
					}
					//==コントロールを作成(デフォルトのクローン)==
					switch (Handle.Name) {
					case "Form":
						Buffer = ResourceManager.GetDefault (typeof(Form));
						Console.WriteLine ("Form made");
						break;
					case "Button":
						Buffer = ResourceManager.GetDefault (typeof(Button));
						break;
					case "Label":
						Buffer = ResourceManager.GetDefault (typeof(Label));
						break;
					case "TextBox":
						Buffer = ResourceManager.GetDefault (typeof(TextBox));
						break;
					case "VScrollBar":
						Buffer = ResourceManager.GetDefault (typeof(VScrollBar));
						break;
					case "ListControl":
						Buffer = ResourceManager.GetDefault (typeof(ListControl<int>));
						break;
					}

					//==コントロールに共通な項目を読み込み==
					//-辞書に登録-
					Current.AddDictionary (Handle.GetAttribute ("Id") ?? "BaseForm", index);
					index++;
					//-サイズなど-
					Buffer.Position = new pointint (Current.Position.x + int.Parse (Handle.GetAttribute ("X")), Current.Position.y + int.Parse (Handle.GetAttribute ("Y")));
					Buffer.Size = new pointint (int.Parse (Handle.GetAttribute ("Width")), int.Parse (Handle.GetAttribute ("Height")));
					Buffer.Text = Handle.GetAttribute ("Text") ?? "";
					Buffer.BaseMargin = Parse (Handle.GetAttribute ("BaseMargin"), Buffer.BaseMargin);

					//==コントロールに特有な値の設定==
					//-マージンの取り込みの改善をすること-
					//TODO Improve how to get margins.
					switch (Handle.Name) {
					case "VScrollBar":
						(Buffer as VScrollBar).ScrollBarMargin = Parse (Handle.GetAttribute ("ScrollBarMargin"), (Buffer as VScrollBar).ScrollBarMargin);
						(Buffer as VScrollBar).ButtonHeight = Parse (Handle.GetAttribute ("ButtonHeight"), (Buffer as VScrollBar).ButtonHeight);
						(Buffer as VScrollBar).Max = Parse (Handle.GetAttribute ("Max"), (Buffer as VScrollBar).Max);
						(Buffer as VScrollBar).Min = Parse (Handle.GetAttribute ("Min"), (Buffer as VScrollBar).Min);
						(Buffer as VScrollBar).Value = Parse (Handle.GetAttribute ("DefaultValue"), (Buffer as VScrollBar).Value);
						break;
					}

					//==スキン==
					//-スキンをセット-
					if (Handle.GetAttribute ("Skin") == "Default" || Handle.GetAttribute("Skin") == null) {
						//何もしない. Nothing to do.
					} else {
						Buffer.SetSkin (Handle.GetAttribute ("Skin"));
					}
					//==コントロールを親に追加==
					if (Buffer != null) {
						Current.AddChild (Buffer);
					}

					//==子コントロールを作るか確認==
					if (!Handle.IsEmptyElement) {
						Console.WriteLine ("Get in child");
						indexS.Push (index);
						index = 0;
						controlS.Push (Current);
						Current = Buffer;
						Buffer.InitializeDictionary ();
					} else {
						index++;
					}
					Buffer = null;
				}
				//Console.WriteLine ("Result of {0}:", XmlPath);

			}
			return Current [0] as Form;
		}

		public static int Parse (string input, int defaultvalue)
		{
			int rv;
			if (int.TryParse (input, out rv)) {
				return rv;
			} else {
				return defaultvalue;
			}
		}
	}
}
