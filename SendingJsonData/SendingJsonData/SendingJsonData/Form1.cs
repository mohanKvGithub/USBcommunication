using SendingJsonData.Model;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;
using System.Management;
using System.Text;

namespace SendingJsonData
{
    public partial class Form1 : Form
    {
        private List<WorkPermitSync> workPermitList= new List<WorkPermitSync>();
        private List<List<WorkerSync>> workerList=new List<List<WorkerSync>>();
        private int workerIndx=0;
        private int workPermitIndx = 0;
        private string curntWorkPermitNo;
        private string curntWorkerNo;
        private Process adbProcess;
        private CancellationTokenSource cancellationTokenSource;
        private ManagementEventWatcher watcher;
        private string reciever = "cqkn7px4vgknqcay";


        public Form1()
        {
            InitializeComponent();
            LoadWorkPermit();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }
        private async void syncDevice()
        {        
                workerIndx = 0;
                workPermitIndx = 0;
                StopHandShakeMonitoring();
                runAdbCommand("logcat -c");
                StartHandShakeMonitoring();
                string command = $"shell am broadcast -a DataFromDesktop --es DataFromDesktop 'NEW_WP_SYNC'";
                runAdbCommand(command);
        }
        private void StopHandShakeMonitoring()
        {
            try
            {
                if (adbProcess != null && !adbProcess.HasExited)
                {
                    adbProcess.Kill();
                    adbProcess.Dispose();
                    adbProcess = null;
                }

                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while stopping handshake monitoring: " + ex.Message);
            }
        }
        private async void StartHandShakeMonitoring()
        {
            try
            {
                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string adbPath = Path.GetFullPath(Path.Combine(currentDirectory, @"..\..\..\", "platform-tools", "adb.exe"));

                adbProcess = new Process();
                adbProcess.StartInfo.FileName = adbPath;
                adbProcess.StartInfo.Arguments = "logcat -s HandShakeMsg";
                adbProcess.StartInfo.RedirectStandardOutput = true;
                adbProcess.StartInfo.UseShellExecute = false;
                adbProcess.StartInfo.CreateNoWindow = true;

                adbProcess.OutputDataReceived += AdbProcess_OutputDataReceived;

                adbProcess.Start();
                adbProcess.BeginOutputReadLine();

                cancellationTokenSource = new CancellationTokenSource();
                await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Handle the cancellation exception if needed
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while starting ADB process: " + ex.Message);
            }
        }
        private async void AdbProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                string responseMsg = ParseLogData(e.Data);

                
                InvokeIfNeeded(() =>
                {
                    if (responseMsg.Equals("READY_NEW_WP_SYNC"))
                    {
                        InvokeIfNeeded(() =>
                        {
                            string cmd = $"shell am broadcast -a DataFromDesktop --es DataFromDesktop 'WP_COUNT, {workPermitList.Count}'";
                            runAdbCommand(cmd);
                           
                        });
                    }
                    else if (responseMsg.Equals("SUCCESS"))
                    {
                        InvokeIfNeeded(() =>
                        {
                            MessageBox.Show("Device Synced Successfully");
                            runAdbCommand("logcat -c");
                        });
                    }
                    else if (responseMsg.Equals("READY_WP_COUNT"))
                    {
                        InvokeIfNeeded(() =>
                        {
                            SendWorkPermit();
                            runAdbCommand("logcat -c");
                        });
                    }
                    else if (responseMsg.Equals($"WP{curntWorkPermitNo}_RECEIVED"))
                    {
                        InvokeIfNeeded(() =>
                        {
                            string cmd = $"shell am broadcast -a DataFromDesktop --es DataFromDesktop 'W_COUNT, {workerList[workPermitIndx].Count}'";
                            runAdbCommand(cmd);
                            runAdbCommand("logcat -c");
                        });
                    }
                    else if (responseMsg.Equals("READY_W_COUNT"))
                    {
                        InvokeIfNeeded(() =>
                        {
                            SendWorker();
                            runAdbCommand("logcat -c");
                        });
                    }
                    else if (responseMsg.Equals($"WP{curntWorkPermitNo}_W{curntWorkerNo}_RECEIVED"))
                    {
                        InvokeIfNeeded(() =>
                        {
                            if (workerIndx >= workerList[workPermitIndx].Count && workPermitIndx + 1 >= workPermitList.Count)
                            {
                                runAdbCommand("shell am broadcast -a DataFromDesktop --es DataFromDesktop 'VERIFY'");
                                runAdbCommand("logcat -c");
                            }
                            else if (workerIndx >= workerList[workPermitIndx].Count)
                            {
                                workPermitIndx = workPermitIndx + 1;
                                workerIndx = 0;
                                SendWorkPermit();
                                runAdbCommand("logcat -c");
                            }
                            else
                            {
                                SendWorker();
                                runAdbCommand("logcat -c");
                            }
                        });
                    }
                    else if (responseMsg.Equals("FAILURE"))
                    {
                        InvokeIfNeeded(() =>
                        {
                            
                            runAdbCommand("logcat -c");
                        });
                    }
                });
            }
        }
        private async Task<string> runAdbCommand(string command)
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string adbPath = Path.GetFullPath(Path.Combine(currentDirectory, @"..\..\..\", "platform-tools", "adb.exe"));
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = adbPath,
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = Process.Start(processInfo))
                {
                    var timeout = TimeSpan.FromSeconds(30);
                    var output = await Task.Run(() =>
                    {
                        var outputBuilder = new StringBuilder();
                        var errorBuilder = new StringBuilder();

                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (e.Data != null) outputBuilder.AppendLine(e.Data);
                        };
                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data != null) errorBuilder.AppendLine(e.Data);
                        };

                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        if (process.WaitForExit((int)timeout.TotalMilliseconds))
                        {
                            return (process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
                        }
                        else
                        {
                            process.Kill();
                            return (-1, outputBuilder.ToString(), "Process timed out");
                        }
                    });

                    if (output.Item1 == 0)
                    {
                        return output.Item2;
                    }
                    else
                    {
                        StopHandShakeMonitoring();
                        return "error running command";
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Exception running command: {ex.Message}");
                StopHandShakeMonitoring();
                return "exception running command";
            }
        }

        private async void InvokeIfNeeded(Action action)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(action);
            }
            else
            {
                action();
            }
        }


        private string ParseLogData(string line)
        {
            if (line.Equals("--------- beginning of main"))
            {
                runAdbCommand("logcat -c");
                return "";
            }
            int stringSeparatorIndx = 0;

            for (int i = line.Length - 1; i >= 0; i--)
            {
                if (line[i] == ':')
                {
                    stringSeparatorIndx = i;
                    break;
                }
            }
            string result = line.Substring(stringSeparatorIndx + 1).Trim();
            return result;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            syncDevice();
        }

        private int LoadWorkers(int workPermitId)
        {
            List<WorkerSync> worker = new List<WorkerSync>();
            worker.Add(new WorkerSync
            {
                Id=1222,
                Name="Suresh",
                Title="Transport",
                Fn="Sri",
                Mn="dev",
                Ln="kumar",
                Status="Active",
                Gndr="Male",
                Email="suresh@gmail.com",
                Dep="Department1",
                BadgNo="343",
                Company="Armco",
                Role="management",
                WpId= workPermitId,
                MobileNo=1294487654
            });

            worker.Add(new WorkerSync
            {
                Id = 1224,
                Name = "Geetha",
                Title = "Finance",
                Fn = "Smt",
                Mn = "devi",
                Ln = "nair",
                Status = "Active",
                Gndr = "Female",
                Email = "geetha@gmail.com",
                Dep = "Department3",
                BadgNo = "345",
                Company = "Armco",
                Role = "accountant",
                WpId = workPermitId,
                MobileNo = 1294487656
            });

            worker.Add(new WorkerSync
            {
                Id = 1225,
                Name = "Arjun",
                Title = "IT",
                Fn = "Sri",
                Mn = "tech",
                Ln = "rao",
                Status = "Inactive",
                Gndr = "Male",
                Email = "arjun@gmail.com",
                Dep = "Department4",
                BadgNo = "346",
                Company = "Armco",
                Role = "developer",
                WpId = workPermitId,
                MobileNo = 1294487657
            });

            worker.Add(new WorkerSync
            {
                Id = 1226,
                Name = "Lakshmi",
                Title = "HR",
                Fn = "Smt",
                Mn = "hr",
                Ln = "reddy",
                Status = "Active",
                Gndr = "Female",
                Email = "lakshmi@gmail.com",
                Dep = "Department5",
                BadgNo = "347",
                Company = "Armco",
                Role = "manager",
                WpId = workPermitId,
                MobileNo = 1294487658
            });

            worker.Add(new WorkerSync
            {
                Id = 1227,
                Name = "Manoj",
                Title = "Operations",
                Fn = "Sri",
                Mn = "op",
                Ln = "singh",
                Status = "Active",
                Gndr = "Male",
                Email = "manoj@gmail.com",
                Dep = "Department6",
                BadgNo = "348",
                Company = "Armco",
                Role = "operator",
                WpId = workPermitId,
                MobileNo = 1294487659
            });
            if(workPermitId== 234)
            {
                worker.RemoveAt(0);
                worker.RemoveAt(1);
            }
            workerList.Add(worker);
            return worker.Count;
        }
        
        private void LoadWorkPermit()
        {
            WorkPermitSync workPermit = new WorkPermitSync();
            int workerCount = 0;
            workPermit = new WorkPermitSync
            {
                Id = 232,
                Dat = DateTime.Now,
                Isuer = "Admin",
                Recver = "Android Device 45",
                Plnt = "New plant",
                Lat = 5.66,
                Long = 945.93,
                Typ = "Cold Work Permit",
                CS = "Active",
                WC = 6,
                Adress = "some address",
                DevicId = "Device 4002"
            };
            workPermitList.Add(workPermit);
            workerCount= LoadWorkers(workPermit.Id);
            workPermitList[workPermitList.Count - 1].WC = workerCount;
            workPermit = new WorkPermitSync
            {
                Id = 234,
                Dat = DateTime.Now,
                Isuer = "Admin",
                Recver = "Android Device 47",
                Plnt = "New plant2",
                Lat = 7.66,
                Long = 949.93,
                Typ = "Cold Work Permit",
                CS = "Active",
                WC = 3,
                Adress = "some address",
                DevicId = "Device 4008"
            };
            workPermitList.Add(workPermit);
            workerCount = LoadWorkers(workPermit.Id);
            workPermitList[workPermitList.Count - 1].WC = workerCount;
        }

        private async void SendWorkPermit()
        {
            int temp = workPermitIndx;
            temp++;
            curntWorkPermitNo = temp.ToString("D2");
            string workPermitJsonStr = JsonSerializer.Serialize(workPermitList[workPermitIndx]);
            string cmd = $"shell am broadcast -a DataFromDesktop --es DataFromDesktop 'WP{curntWorkPermitNo}_SYNC, {workPermitJsonStr}'";
            runAdbCommand(cmd);
        }

        private async void SendWorker()
        {
            workerIndx=workerIndx+1;
            curntWorkerNo = workerIndx.ToString("D3");
            string workerJsonStr = JsonSerializer.Serialize(workerList[workPermitIndx][workerIndx - 1]);
            string cmd = $"shell am broadcast -a DataFromDesktop --es DataFromDesktop 'WP{curntWorkPermitNo}_W{curntWorkerNo}_SYNC, {workerJsonStr}'";
            runAdbCommand(cmd);
        }

        private void InitializeUSBWatcher()
        {
            try
            {
                // Query for USB removal events
                WqlEventQuery query = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");

                watcher = new ManagementEventWatcher(query);
                watcher.EventArrived += new EventArrivedEventHandler(USBUnplugEventArrived);
                watcher.Start();
            }
            catch (ManagementException ex)
            {
                MessageBox.Show("An error occurred while trying to create the event watcher: " + ex.Message);
            }
        }
        private void USBUnplugEventArrived(object sender, EventArrivedEventArgs e)
        {

            //MessageBox.Show("A USB device was unplugged.");
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (watcher != null)
            {
                watcher.Stop();
                watcher.Dispose();
            }
            base.OnFormClosing(e);
        }

        private bool isDeviceConnected()
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string adbPath = Path.GetFullPath(Path.Combine(currentDirectory, @"..\..\..\", "platform-tools", "adb.exe"));

                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = "devices",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var adbProcess = new Process())
                {
                    adbProcess.StartInfo = processInfo;
                    adbProcess.Start();
                    string output =  adbProcess.StandardOutput.ReadToEnd();
                    string error =  adbProcess.StandardError.ReadToEnd();
                    adbProcess.WaitForExit();
                if (output.Contains(reciever))
                    return true;
                }
            return false;
        }
    }
}
