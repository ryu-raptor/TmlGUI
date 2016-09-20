using System;
using DxLibDLL;
using System.Collections.Generic;


namespace ThlGUI
{
	public delegate void EventHandler(Control sender, EventArgs args);
	
	public struct pointint
	{
		public int x;
		public int y;

		public static pointint Get(int x, int y)
		{
			pointint rv;
			rv.x = x;
			rv.y = y;
			return rv;
		}

		public static bool Equals(pointint A, pointint B)
		{
			if ((A.x == B.x) && (A.y == B.y))
				return true;
			else
				return false;
		}
	}

	public struct GUIMessage
	{
		public int Message;
		public byte[] KeyBuffer;
		public byte[] KeyBufferMask;
		public pointint MousePoint;
		public int MouseClickState;
		public int MouseWheelInput;

		public void AddMessage(int message)
		{
			this.Message = this.Message | message;
			return;
		}

		public void RemoveMessage(int message)
		{
			this.Message = this.Message & (~message);
			return;
		}

		public void CopyTo(out GUIMessage direction)
		{
			direction.Message = Message;
			direction.MousePoint = MousePoint;
			direction.MouseClickState = MouseClickState;
			direction.MouseWheelInput = MouseWheelInput;
			direction.KeyBuffer = KeyBuffer.Clone () as byte[];
			direction.KeyBufferMask = KeyBufferMask.Clone () as byte[];
			return;
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
		public static int DrawWrapString (int x, int y, int textheight, int wraplength, string text, uint textcolor)
		{
			if (text == null)
				return -1;
			//文字列の分割
			var Buffer = GenerateWrapString(wraplength, text);

			//描画
			int rv = 0;
			foreach (var alge in Buffer) {
				rv *= DX.DrawString (x, y, alge, textcolor) + 1;
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
		public static int DrawWrapString (int x, int y, int textheight, string[] text, uint textcolor)
		{
			//描画
			int rv = 0;
			foreach (var alge in text) {
				rv *= DX.DrawString (x, y, alge, textcolor) + 1;
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
			foreach (var alge in Buffer) {
				Console.WriteLine (alge);
			}
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
						MiniBuf = Buffer[index].Substring (startp);
					} else {
						MiniBuf = Buffer[index].Substring (startp, wraplength);
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

	public class ResourceManager
	{
		public const int MaxResourceForControls = 4;
		public const int ResourceForControl = 0;
		public const int ResourceForForm = 1;
		public const int ResourceForButton = 3;
		public const int ResourceForTextBox = 4;

		public static int[][] ResourceHandle = new int[MaxResourceForControls][];

		/// <summary>
		/// Opens the resource file.
		/// </summary>
		/// <returns>Success = 0, Failed = -1.</returns>
		/// <param name="path">Path to the resource file(.rsc).</param>
		public static int OpenResource(string path)
		{
			return 0;
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
		private static Func<byte[]> KeyboardGettingMethod;
		private static Func<int,bool> KeyboardStateGettingMethod;

		private static List<Form> Collections = new List<Form>();
		private static Form FocusedForm;
		
		private static GUIMessage Message;

		public GraphicalUI()
		{
		}

		public static void GraphicalUIinit()
		{
			Message.KeyBuffer = new byte[256];
			Message.KeyBufferMask = new byte[256];
		}

		//CollectionsにFormを追加する。zソートは今はしない
		public static int Add(Form form)
		{
			Collections.Add(form);
			return 0;
		}

		//アクティブなFormを最前面に持っていく
		public static int SortZBuffer()
		{
			Form Buffer;
			for (int i = 0; i < Collections.Count; i++)
			{
				if (Collections[i] == FocusedForm) {
					Buffer = Collections[i];
					Collections.RemoveAt(i);
					Collections.Add(Buffer);
					break;
				}
			}
			return 0;
		}

		//formをFocusedにする(前代のFocusを解除する)
		public static void SetFocus(Form form)
		{
			FocusedForm.Unfocus(null, null);
			FocusedForm = form;
			return;
		}

		public static int SetMousePointGettingMethod(Func<pointint> method)
		{
			MousePointGettingMethod += method;
			return 0;
		}

		public static int SetMouseClickGettingMethod(Func<int> method)
		{
			MouseClickGettingMethod += method;
			return 0;
		}

		public static int SetKeyboardGettingMethod(Func<byte[]> method)
		{
			KeyboardGettingMethod += method;
			return 0;
		}
		public static int SetKeyboardStateGettingMethod(Func<int,bool> method)
		{
			KeyboardStateGettingMethod += method;
			return 0;
		}

		public static int Routine()
		{
			//1.Making Message
			GUIMessage Msg = new GUIMessage();
			//Collect inputs
			Msg.MousePoint = MousePointGettingMethod();
			Msg.MouseClickState = MouseClickGettingMethod();
			Msg.KeyBuffer = KeyboardGettingMethod();
			Msg.KeyBufferMask = Message.KeyBuffer.Clone () as byte[];
			//CheckDifferences
			//Mouse
			//Cursor
			if (!pointint.Equals(Msg.MousePoint, Message.MousePoint)) {
				Msg.AddMessage(Message_Cursor_Move);
			}
			//Button
			if ((Msg.MouseClickState & DX.MOUSE_INPUT_LEFT) != 0)
			{
				Msg.AddMessage(Message_Mouse_LeftClick);
				//クリック判定(長押しでない)
				if (Message.MouseClickState != Msg.MouseClickState) {
					Msg.AddMessage(Message_Mouse_Click);
				}
			}
			if ((Msg.MouseClickState & DX.MOUSE_INPUT_RIGHT) != 0)
			{
				Msg.AddMessage(Message_Mouse_RightClick);
				//クリック判定(長押しでない)
				if (Message.MouseClickState != Msg.MouseClickState) {
					Msg.AddMessage(Message_Mouse_Click);
				}
			}
			if ((Msg.MouseClickState & DX.MOUSE_INPUT_MIDDLE) != 0)
			{
				Msg.AddMessage(Message_Mouse_MiddleClick);
				//クリック判定(長押しでない)
				if (Message.MouseClickState != Msg.MouseClickState) {
					Msg.AddMessage(Message_Mouse_Click);
				}
			}
			//Wheel
			if (Msg.MouseWheelInput != 0) {
				Msg.AddMessage(Message_MouseWheel_Role);
			}

			//Keyboard
			//省略

			//2.Sending Message
			for (int i = 0; i < Collections.Count; i++) {
				Collections [i].SendMessage (Msg);
				Collections [i].ProcessMessasge ();
			}

			//Zソート
			if ((Message.Message & Message_Mouse_Click) != 0) {
				SortZBuffer();
			}

			//3.Copy message to buffer
			Message.Message = Msg.Message;
			Message.MouseClickState = Msg.MouseClickState;
			Message.MousePoint = Msg.MousePoint;
			Msg.KeyBuffer.CopyTo(Message.KeyBuffer, 0);
			Message.MouseWheelInput = Msg.MouseWheelInput;

			//4.描画命令(Collectionsはzソートされているのでそのまま描画できる)
			foreach (Control alge in Collections)
			{
				alge.Render();
			}
			return 0;
		}
	}

	public class Control
	{
		protected pointint Position;
		protected pointint Size;
		protected List<Control> Collections = new List<Control>();
		protected Control ParentControl;
		protected string Label;
		protected GUIMessage Message = new GUIMessage();

		//Skins
		protected uint TextColor = DX.GetColor (0, 0, 0);
		protected static readonly int AutoSkinMarge = 4; //周りのマージ(px)
		protected int SkinTop;
		protected int SkinBottom;
		protected int SkinRight;
		protected int SkinLeft;
		protected int SkinTopLeft;
		protected int SkinTopRight;
		protected int SkinBottomLeft;
		protected int SkinBottomRight;
		protected int SkinBase;

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

		//Properties
		public virtual bool Focused { get; private set; } = false;

		//カーソルがControlの上に乗っているときにtrue
		public virtual bool Pointed { get; private set; } = false;

		public virtual string Text { get; set; } = "";


		public Control()
		{
			//Focusをイベントに関連付ける
			GotFocus += Focus;
			LostFocus += Unfocus;
		}

		/// <summary>
		/// Null例外を避けるイベントInvokeメソッドです.イベントはこれを経由してInvokeすることを推奨します.
		/// </summary>
		/// <param name="e">イベント</param>
		/// <param name="sender">呼び出し主</param>
		/// <param name="args">引数</param>
		public void InvokeEvent(EventHandler e, Control sender, EventArgs args)
		{
			e ?.Invoke (sender, args);
			return;
		}

		public virtual void AddChild(Control control)
		{
			Collections.Add (control);
			return;
		}

		public virtual void RemoveChild(Control control)
		{
			for (int i = 0; i < Collections.Count; i++) {
				if (Collections[i] == control) {
					Collections.RemoveAt (i);
					break;
				}
			}
			return;
		}

		public virtual void SendMessage(GUIMessage message)
		{
			message.CopyTo (out Message);
			//Collectionsにもメッセージを送ります
			foreach (Control alge in Collections)
			{
				alge.SendMessage(message);
			}
		}

		public virtual void SetBounds(int x, int y, int width, int height)
		{
			Position = pointint.Get(x, y);
			Size = pointint.Get(width, height);
			return;
		}

		/// <summary>
		/// スキン用に画像を切り抜き設定します。
		/// </summary>
		/// <param name="GHandle">グラフィックハンドル(DxLib)</param>
		public virtual void SetSkin(int GHandle)
		{
			pointint GSize;
			DX.GetGraphSize (GHandle, out GSize.x, out GSize.y);
			SkinTop = DX.DerivationGraph (AutoSkinMarge, 0, GSize.x - (2 * AutoSkinMarge), AutoSkinMarge, GHandle);
			SkinBottom = DX.DerivationGraph (AutoSkinMarge, GSize.y - AutoSkinMarge, GSize.x - (2 * AutoSkinMarge), AutoSkinMarge, GHandle);
			SkinLeft = DX.DerivationGraph (0, AutoSkinMarge, AutoSkinMarge, GSize.y - (2 * AutoSkinMarge), GHandle);
			SkinRight = DX.DerivationGraph (GSize.x - AutoSkinMarge, AutoSkinMarge, AutoSkinMarge, GSize.y - (2 * AutoSkinMarge), GHandle);
			SkinTopLeft = DX.DerivationGraph (0, 0, AutoSkinMarge, AutoSkinMarge, GHandle);
			SkinTopRight = DX.DerivationGraph (GSize.x - AutoSkinMarge, 0, AutoSkinMarge, AutoSkinMarge, GHandle);
			SkinBottomLeft = DX.DerivationGraph (0, GSize.y - AutoSkinMarge, AutoSkinMarge, AutoSkinMarge, GHandle);
			SkinBottomRight = DX.DerivationGraph (GSize.x - AutoSkinMarge, GSize.y - AutoSkinMarge, AutoSkinMarge, AutoSkinMarge, GHandle);
			SkinBase = DX.DerivationGraph (AutoSkinMarge, AutoSkinMarge, GSize.x - (2 * AutoSkinMarge), GSize.y - (2 * AutoSkinMarge), GHandle);
			return;
		}

		public virtual void SetSkin(string GPath)
		{
			SetSkin (DX.LoadGraph (GPath));
			return;
		}

		public virtual int Render()
		{
			//ベースを描く
			DX.DrawExtendGraph (Position.x + AutoSkinMarge, Position.y + AutoSkinMarge, Position.x + Size.x - AutoSkinMarge, Position.y + Size.y - AutoSkinMarge, SkinBase, DX.TRUE);
			//枠を描く
			DX.DrawExtendGraph (Position.x, Position.y + AutoSkinMarge, Position.x + AutoSkinMarge, Position.y + Size.y - AutoSkinMarge, SkinLeft, DX.TRUE);
			DX.DrawExtendGraph (Position.x + Size.x - AutoSkinMarge, Position.y + AutoSkinMarge, Position.x + Size.x, Position.y + Size.y - AutoSkinMarge, SkinRight, DX.TRUE);
			DX.DrawExtendGraph (Position.x + AutoSkinMarge, Position.y, Position.x + Size.x - AutoSkinMarge, Position.y + AutoSkinMarge, SkinTop, DX.TRUE);
			DX.DrawExtendGraph (Position.x + AutoSkinMarge, Position.y + Size.y - AutoSkinMarge, Position.x + Size.x - AutoSkinMarge, Position.y + Size.y, SkinBottom, DX.TRUE);
			DX.DrawGraph (Position.x, Position.y, SkinTopLeft, DX.TRUE);
			DX.DrawGraph (Position.x, Position.y + Size.y - AutoSkinMarge, SkinBottomLeft, DX.TRUE);
			DX.DrawGraph (Position.x + Size.x - AutoSkinMarge, Position.y, SkinTopRight, DX.TRUE);
			DX.DrawGraph (Position.x + Size.x - AutoSkinMarge, Position.y + Size.y - AutoSkinMarge, SkinBottomRight, DX.TRUE);
			return 0;
		}

		public virtual int ProcessMessasge()
		{
			//メッセージ処理をここに記述します。イベントの発生もここで行います。
			//Message_Mouse_Click
			if ((Message.Message & GraphicalUI.Message_Mouse_Click) != 0)
			{
				if (Pointed) {
					if (!Focused) {
						InvokeEvent (GotFocus, this, null);
					}
					InvokeEvent (Click, this, null);
				} else if (Focused) {
					InvokeEvent (LostFocus, this, null);
				}
			}
			//Message_Cursor_Move
			if  ((Message.Message & GraphicalUI.Message_Cursor_Move) != 0)
			{
				if ((Message.MousePoint.x >= Position.x) && (Message.MousePoint.x <= Position.x + Size.x) &&
					(Message.MousePoint.y >= Position.y) && (Message.MousePoint.y <= Position.y + Size.y)) {
					if (!Pointed) {
						Pointed = true;
					}
				}
				else {
					if (Pointed) {
						Pointed = false;
						InvokeEvent(MouseLeave, this, null);
					}
				}
			}
			//Mouse Hover
			if (Pointed) {
				InvokeEvent(MouseHover, this, null);
			}

			//Collectionsのメッセージ処理も行います
			for (int i = 0; i < Collections.Count; i++) {
				Collections [i].ProcessMessasge ();
			}
			return 0;
		}


		public virtual void Focus(Control sender, EventArgs args)
		{
			Focused = true;
			return;
		}

		public virtual void Unfocus(Control sender, EventArgs args)
		{
			Focused = false;
			return;
		}
	}

	public class Form : Control
	{
		//Skins
		protected int SkinClose;
		protected int SkinMaximize;
		protected int SkinMinimize;
		protected int SkinStore;
		protected int SkinTitleBarTop;
		protected int SkinTitleBarBottom;
		protected int SkinTitleBarRight;
		protected int SkinTitleBarLeft;
		protected int SkinTitleBarBottomLeft;
		protected int SkinTitleBarBottomRight;

		//Design
		protected pointint TitlebarTextOfset;

		public Form()
		{
			//Default setting of Form.
			Position.x = 0;
			Position.y = 0;
			Size.x = 300;
			Size.y = 300;
			Text = "";
		}

		public Form(int px, int py, int width, int height, string label)
		{
			Position.x = px;
			Position.y = py;
			Size.x = width;
			Size.y = height;
			Text = label;
			//Experimantal==========
			SetSkin (@"./button.png");
			//======================
		}

		public override int Render()
		{
			base.Render ();
			DX.DrawString (Position.x + TitlebarTextOfset.x, Position.y + TitlebarTextOfset.y, Text, DX.GetColor (0, 0, 0));
			foreach (var alge in Collections) {
				alge.Render ();
			}
			return 0;
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
				StrWidth = DX.GetDrawStringWidth (Text, Text.Length);
				TextUpLeft.x = (Size.x - StrWidth) / 2;
				TextUpLeft.y = (Size.y - StrHeight) / 2;
			}
		}

		public Button()
		{
			Position = pointint.Get (0, 0);
			Size = pointint.Get (200, 50);

			//Experimantal=======
			SetSkin (@"./button.png");
			//===================
		}

		public override int Render()
		{
			base.Render ();
			DX.DrawString (Position.x + TextUpLeft.x, Position.y + TextUpLeft.y, Text, TextColor);
			return 0;
		}
	}

	public class Textbox : Control
	{
		private pointint TextUpLeft;
		private int InputHandle = -1;
		private uint BufferSize;
		private int SingleCharOnlyFlag = DX.FALSE;
		private int NumberCharOnlyFlag = DX.FALSE;
		private System.Text.StringBuilder Buffer;

		public Textbox()
		{
			BufferSize = 256;
			Position = pointint.Get (0, 0);
			Size = pointint.Get (200, 20);
			Text = "Input Await";
			GotFocus += InputAwait;
			LostFocus += InputUnawait;
		}

		private void InputAwait(Control sender, EventArgs args)
		{
			if (InputHandle == -1) {
				InputHandle = DX.MakeKeyInput (BufferSize, DX.FALSE, SingleCharOnlyFlag, NumberCharOnlyFlag);
			}
			DX.SetActiveKeyInput (InputHandle);
			DX.SetKeyInputString (Text, InputHandle);
			return;
		}

		private void InputUnawait(Control sender, EventArgs args)
		{
			Text = GetBuffer ();
			DX.DeleteKeyInput (InputHandle);
			InputHandle = -1;
			return;
		}

		private string GetBuffer()
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

		public override int Render()
		{
			Text = GetBuffer ();
			base.Render ();
			DX.DrawString (Position.x + TextUpLeft.x, Position.y + TextUpLeft.y, Text, TextColor);
			foreach (var alge in Collections) {
				alge.Render ();
			}
			return 0;
		}

	}

	public class Label : Control
	{
	}

	public class ScrollBar : Control
	{
	}

	public class HScrollBar : ScrollBar
	{
	}

	public class VScrollBar : ScrollBar
	{
	}

	public class ListControl : Control
	{
	}

	public class MessageBox : Form
	{
		private Button ResponceButton;
		private string[] WrappedMessage;
		private int WrapLength;
		private pointint WrapBoxSize;

		public MessageBox (string Message) : base (0, 0, 250, 200, "Message")
		{
			Console.WriteLine (DX.GetDrawStringWidth (Message, Message.Length).ToString () + "/" + Message.Length.ToString ());
			double CharWidth = (double)DX.GetDrawStringWidth (Message, Message.Length) / Message.Length;
			WrapLength = (int)(200 / CharWidth); //ボックスの両端から25pxずつ引いたボックスのサイズで計算
			WrapBoxSize.x = (int)(WrapLength * CharWidth);

			WrappedMessage = DXEx.GenerateWrapString (WrapLength, Message);
			ResponceButton = new Button ();
			ResponceButton.SetBounds (15, 160, 100, 25);
			AddChild (ResponceButton);
			ResponceButton.Click += ResponceButton_Click;
		}

		void ResponceButton_Click (Control sender, EventArgs args)
		{
			Console.WriteLine (WrapBoxSize.x);
			Console.WriteLine (WrapLength);
			Console.WriteLine (sender);
		}

		public override int Render()
		{
			base.Render ();
			DXEx.DrawWrapString (Position.x + (Size.x - WrapBoxSize.x) / 2, 10, 20, WrappedMessage, TextColor);
			foreach (var alge in Collections) {
				alge.Render ();
			}
			return 0;
		}
	}
}
