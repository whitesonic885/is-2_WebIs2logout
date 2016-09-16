using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Services;

namespace is2logout
{
	/// <summary>
	/// [is2logout]
	/// </summary>
	//--------------------------------------------------------------------------
	// 修正履歴
	//--------------------------------------------------------------------------
	// MOD 2007.01.09 東都）高木 ログ出力の強化 
	// MOD 2007.04.20 東都）高木 ログ出力の強化 
	//--------------------------------------------------------------------------
	// MOD 2010.10.07 東都）高木 ログバッファの拡張 
	//--------------------------------------------------------------------------
	[System.Web.Services.WebService(
		 Namespace="http://Walkthrough/XmlWebServices/",
		 Description="is2logout")]

	public class Service1 : System.Web.Services.WebService
	{
		private static string sLogPath = "";

		private static Thread trLogOut = null;
		private static uint uiLogOut = 0;
		private static uint uiBuff   = 0;
		private static bool bSetLog = false;
		private static bool bGetLog = false;
// MOD 2007.04.20 東都）高木 ログ出力の強化 START
//		private static string[] sBuff = new string[256];
// MOD 2010.10.07 東都）高木 ログバッファの拡張 START
//		private static string[] sBuff = new string[4096];
		private static string[] sBuff = new string[16 * 1024];
// MOD 2010.10.07 東都）高木 ログバッファの拡張 END
// MOD 2007.04.20 東都）高木 ログ出力の強化 END
		private static Encoding enc = Encoding.GetEncoding("shift-jis");

		public Service1()
		{
			//CODEGEN: この呼び出しは、ASP.NET Web サービス デザイナで必要です。
			InitializeComponent();

			if(sLogPath == null || sLogPath.Length == 0)
			{
				// ログ出力パスの取得
//				System.Type type = System.Type.GetType("System.String");
//				System.Configuration.AppSettingsReader config = new System.Configuration.AppSettingsReader();
//				sLogPath = config.GetValue("path", type).ToString();
				object obj = null;
				obj = Context.Application.Get("sLogPath");
				if(obj != null) sLogPath = (string)obj;
			}

// MOD 2007.04.20 東都）高木 ログ出力の強化 START
//			if(trLogOut == null)
			if(trLogOut == null || trLogOut.IsAlive == false)
// MOD 2007.04.20 東都）高木 ログ出力の強化 END
			{
				// ログ出力スレッドを開始
				trLogOut = new Thread(new ThreadStart(ThreadLogOut));
				trLogOut.IsBackground = true;
				trLogOut.Start();
			}
		}

		#region コンポーネント デザイナで生成されたコード 
		
		//Web サービス デザイナで必要です。
		private IContainer components = null;
				
		/// <summary>
		/// デザイナ サポートに必要なメソッドです。このメソッドの内容を
		/// コード エディタで変更しないでください。
		/// </summary>
		private void InitializeComponent()
		{
		}

		/// <summary>
		/// 使用されているリソースに後処理を実行します。
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if(disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);		
		}
		
		#endregion

		/*********************************************************************
		 * ログ出力用バッファへの書込み
		 * 引数：ログ出力文字列
		 * 戻値：無し
		 *
		 *********************************************************************/
		[WebMethod]
		public int LogOut(string sLog)
		{
			if(sLog == null || sLog.Length == 0) return 0;
// MOD 2007.01.09 東都）高木 ログ出力の強化 START
//			// 簡易排他待ち（10秒）
//			for(uint uiCnt = 0; bSetLog && uiCnt < 50; uiCnt++)
//				Thread.Sleep(200);
//			if(bSetLog) return -1;
			if(SetLogState()) return -1;
//			if(SetLogState()) return 0;
// MOD 2007.01.09 東都）高木 ログ出力の強化 END
// MOD 2007.01.09 東都）高木 ログ出力の強化 START
			int iRet = LogOutSub(sLog);

			ClearLogState();
			return iRet;
		}

		/*********************************************************************
		 * ログ出力用バッファへの書込み
		 * 引数：ログ出力文字列
		 * 戻値：無し
		 *
		 *********************************************************************/
		private static int LogOutSub(string sLog)
		{
// MOD 2007.01.09 東都）高木 ログ出力の強化 END

// MOD 2007.01.09 東都）高木 ログ出力の強化 START
//			bSetLog = true;
// MOD 2007.01.09 東都）高木 ログ出力の強化 END
			try
			{
// MOD 2007.01.09 東都）高木 ログ出力の強化 START
//				// 既に書き込まれている場合には待つ
//				// 簡易排他待ち（10秒）
//				for(uint uiCnt = 0; sBuff[uiBuff] != null && uiCnt < 50; uiCnt++)
//					Thread.Sleep(200);
				//（１周回っている場合）
				if(sBuff[uiBuff] != null) return -2;
//				if(sBuff[uiBuff] != null) return 0;
// MOD 2007.01.09 東都）高木 ログ出力の強化 END
				sBuff[uiBuff++] = sLog;
				if(uiBuff >= sBuff.Length) uiBuff = 0;
			}
// MOD 2007.01.09 東都）高木 ログ出力の強化 START
			catch
			{
//保留　ログ出力
			}
// MOD 2007.01.09 東都）高木 ログ出力の強化 END
			finally
			{
// MOD 2007.01.09 東都）高木 ログ出力の強化 START
//				bSetLog = false;
				ClearLogState();
// MOD 2007.01.09 東都）高木 ログ出力の強化 END
			}

			return 0;
		}

// MOD 2007.01.09 東都）高木 ログ出力の強化 START
		/*********************************************************************
		 * ログ出力ステータス設定
		 * 引数：なし
		 * 戻値：なし
		 *
		 *********************************************************************/
		private static bool SetLogState()
		{
			if(bSetLog) return true;
			bSetLog = true;
			return false;
		}
		private static void ClearLogState()
		{
			bSetLog = false;
		}
// MOD 2007.01.09 東都）高木 ログ出力の強化 END
		/*********************************************************************
		 * ログ出力用バッファからの取出し
		 * 引数：無し
		 * 戻値：ログ出力文字列
		 *
		 *********************************************************************/
		private static string GetLog()
		{
// MOD 2007.01.09 東都）高木 ログ出力の強化 START
//			// 簡易排他待ち（10秒）
//			for(uint uiCnt = 0; bGetLog && uiCnt < 50; uiCnt++)
//				Thread.Sleep(200); 
// MOD 2007.01.09 東都）高木 ログ出力の強化 END
			if(bGetLog)
			{
				bGetLog = false;
// MOD 2007.01.09 東都）高木 ログ出力の強化 START
//				return "[GetLog]エラー：簡易排他待ち時間切れ";
				return "[GetLog]エラー：他のプロセスで使用中";
// MOD 2007.01.09 東都）高木 ログ出力の強化 END
			}

			bGetLog = true;
			string sRet;
			try
			{
				sRet = sBuff[uiLogOut];
				if(sRet != null)
				{
					sBuff[uiLogOut] = null;
					uiLogOut++;
					if(uiLogOut >= sBuff.Length) uiLogOut = 0;
				}
			}
			finally
			{
				bGetLog = false;
			}
			return sRet;
		}

		/*********************************************************************
		 * ログ出力スレッド
		 * 引数：無し
		 * 戻値：無し
		 *
		 *********************************************************************/
		private static void ThreadLogOut()
		{
			string sLog;
			while(true)
			{
				// ログバッファよりデータを取得する
				sLog = GetLog();
				if(sLog == null)
				{
					Thread.Sleep(500); // 0.5秒
					continue;
				}

				System.IO.FileStream   cfs = null;
				System.IO.StreamWriter csw = null;
				string sFileName = sLogPath
								 + System.DateTime.Now.ToString("MMdd")
								 + "_is2LogOut.log";

				try
				{
					// ファイルオープン
					cfs = new System.IO.FileStream(sFileName, 
													System.IO.FileMode.Append, 
													System.IO.FileAccess.Write, 
													System.IO.FileShare.Write);
					csw = new System.IO.StreamWriter(cfs, enc);

					// ログバッファにたまっているものを全て出力する
// MOD 2010.10.07 東都）高木 ログバッファの拡張 START
//					while(sLog != null)
//					{
					uint uiCnt = 0;
					while(sLog != null){
						uiCnt++;
						//無限ループを防ぐ
						if(uiCnt > sBuff.Length){
							break;
						}
// MOD 2010.10.07 東都）高木 ログバッファの拡張 END
						csw.WriteLine(sLog);
						csw.Flush();
						sLog = GetLog();
					}
				}
// MOD 2007.01.09 東都）高木 ログ出力の強化 START
				catch
				{
//保留　ログ出力
				}
// MOD 2007.01.09 東都）高木 ログ出力の強化 END
				finally
				{
					// ファイルクローズ
					if(csw != null) csw.Close();
					csw = null;
					if(cfs != null) cfs.Close();
					cfs = null;
				}
			}
		}
	}
}
