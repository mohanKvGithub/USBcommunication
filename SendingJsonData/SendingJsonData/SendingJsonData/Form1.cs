using SendingJsonData.Model;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;
using System.Management;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SendingJsonData
{
    public partial class Form1 : Form
    {
        private List<WorkPermit> workPermitList = new List<WorkPermit>();
        private List<List<Employee>> workerList = new List<List<Employee>>();
        private int workerIndx = 0;
        private int workPermitIndx = 0;
        private string curntWorkPermitNo;
        private string curntWorkerNo;
        private int workPermitCount = 0;
        private int workerCount = 0;
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
            textBox1.Multiline = true;

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
                string[] splitString = responseMsg.Split(",", 2);

                string data;
                responseMsg = splitString[0];


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
                    //Data sync from android to Desktop
                    switch (responseMsg)
                    {
                        case "UPDATE_WP_SYNC":
                            InvokeIfNeeded(() =>
                            {
                                string cmd = $"shell am broadcast -a DataFromDesktop --es DataFromDesktop 'GET_WP_COUNT'";
                                runAdbCommand(cmd);
                                runAdbCommand("logcat -c");
                            });
                            break;
                        case "WP_COUNT_TABLET":
                            InvokeIfNeeded(() =>
                            {
                                textBox1.Text += splitString[0] + ",  " + splitString[1] + Environment.NewLine;
                                data = splitString[1];
                                workPermitCount = Int32.Parse(data.Trim());
                                workPermitIndx++;
                                curntWorkPermitNo = workPermitIndx.ToString("D2");
                                string cmd = $"shell am broadcast -a DataFromDesktop --es DataFromDesktop 'GET_WP{curntWorkPermitNo}_TABLET'";
                                runAdbCommand(cmd);
                                runAdbCommand("logcat -c");

                            });
                            break;
                        case "W_SYNC_TABLET":
                            InvokeIfNeeded(() =>
                            {
                                textBox1.Text += splitString[0] + ",  " + splitString[1] + Environment.NewLine;
                                data = splitString[1];
                                workerCount = Int32.Parse(data.Trim());
                                curntWorkerNo = workerIndx.ToString("D3");
                                string cmd = $"shell am broadcast -a DataFromDesktop --es DataFromDesktop 'GET_WP{curntWorkPermitNo}_W{curntWorkerNo}_TABLET'";
                                runAdbCommand(cmd);
                                runAdbCommand("logcat -c");

                            });
                            break;
                        case "SUCCESS_CLEAR":
                            InvokeIfNeeded(() =>
                            {

                                runAdbCommand("logcat -c");
                            });
                            break;
                        default:
                            if (responseMsg.Equals($"WP{curntWorkPermitNo}_SYNC_TABLET"))
                            {
                                InvokeIfNeeded(() =>
                                {
                                    textBox1.Text += splitString[0] + ",  " + splitString[1] + Environment.NewLine;
                                    workerIndx = 1;
                                    string cmd = $"shell am broadcast -a DataFromDesktop --es DataFromDesktop 'WP{curntWorkPermitNo}_TABLET_RECEIVED'";
                                    runAdbCommand(cmd);
                                    runAdbCommand("logcat -c");
                                });
                            }
                            else if (responseMsg.Equals($"WP{curntWorkPermitNo}_W{curntWorkerNo}_SYNC_TABLET"))
                            {
                                InvokeIfNeeded(() =>
                                {


                                    string cmd;
                                    if (workerIndx >= workerCount && workPermitIndx >= workPermitCount)
                                    {
                                        textBox1.Text += splitString[0] + ",  " + splitString[1] + Environment.NewLine;
                                        cmd = $"shell am broadcast -a DataFromDesktop --es DataFromDesktop 'CLEAR_TABLET'";
                                        runAdbCommand(cmd);
                                        runAdbCommand("logcat -c");
                                    }
                                    else if (workerIndx >= workerCount)
                                    {
                                        textBox1.Text += splitString[0] + ",  " + splitString[1] + Environment.NewLine;
                                        workPermitIndx++;
                                        curntWorkPermitNo = workPermitIndx.ToString("D2");
                                        cmd = $"shell am broadcast -a DataFromDesktop --es DataFromDesktop 'GET_WP{curntWorkPermitNo}_TABLET'";
                                        runAdbCommand(cmd);
                                        runAdbCommand("logcat -c");
                                    }
                                    else
                                    {
                                        workerIndx++;
                                        curntWorkerNo = workerIndx.ToString("D3");
                                        curntWorkPermitNo = workPermitIndx.ToString("D2");
                                        textBox1.Text += splitString[0] + ",  " + splitString[1] + Environment.NewLine;
                                        cmd = $"shell am broadcast -a DataFromDesktop --es DataFromDesktop 'GET_WP{curntWorkPermitNo}_W{curntWorkerNo}_TABLET'";
                                        runAdbCommand(cmd);
                                        runAdbCommand("logcat -c");
                                    }
                                });
                            }
                            break;
                    };

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
            List<Employee> employees = new List<Employee>();
            employees.Add(new Employee
            {
                Id = 1222,
                FirstName = "Sri",
                MiddleName = "dev",
                LastName = "kumar",
                Name = "Suresh",
                Role = "management",
                IsActive = true,
                ImageUrl = null, // Assuming no image URL is provided
                PhoneNo = 1294487654,
                Email = "suresh@gmail.com",
                DepartmentId = 1, // Assigning a default value or according to your logic
                JobTitle = "Transport",
                WorkId = workPermitId,
                DepartmentName = "Department1",
                Gender = "Male",
                BadgeNo = "343",
                CompanyName = "Armco",
                CreatedOn = DateTime.Now // You may adjust this to the actual creation date
            });

            employees.Add(new Employee
            {
                Id = 1224,
                FirstName = "Smt",
                MiddleName = "devi",
                LastName = "nair",
                Name = "Geetha",
                Role = "accountant",
                IsActive = true,
                ImageUrl = null,
                PhoneNo = 1294487656,
                Email = "geetha@gmail.com",
                DepartmentId = 3,
                JobTitle = "Finance",
                WorkId = workPermitId,
                DepartmentName = "Department3",
                Gender = "Female",
                BadgeNo = "345",
                CompanyName = "Armco",
                CreatedOn = DateTime.Now
            });

            employees.Add(new Employee
            {
                Id = 1225,
                FirstName = "Sri",
                MiddleName = "tech",
                LastName = "rao",
                Name = "Arjun",
                Role = "developer",
                IsActive = false,
                ImageUrl = null,
                PhoneNo = 1294487657,
                Email = "arjun@gmail.com",
                DepartmentId = 4,
                JobTitle = "IT",
                WorkId = workPermitId,
                DepartmentName = "Department4",
                Gender = "Male",
                BadgeNo = "346",
                CompanyName = "Armco",
                CreatedOn = DateTime.Now
            });

            employees.Add(new Employee
            {
                Id = 1226,
                FirstName = "Smt",
                MiddleName = "hr",
                LastName = "reddy",
                Name = "Lakshmi",
                Role = "manager",
                IsActive = true,
                ImageUrl = null,
                PhoneNo = 1294487658,
                Email = "lakshmi@gmail.com",
                DepartmentId = 5,
                JobTitle = "HR",
                WorkId = workPermitId,
                DepartmentName = "Department5",
                Gender = "Female",
                BadgeNo = "347",
                CompanyName = "Armco",
                CreatedOn = DateTime.Now
            });

            employees.Add(new Employee
            {
                Id = 1227,
                FirstName = "Sri",
                MiddleName = "op",
                LastName = "singh",
                Name = "Manoj",
                Role = "operator",
                IsActive = true,
                ImageUrl = null,
                PhoneNo = 1294487659,
                Email = "manoj@gmail.com",
                DepartmentId = 6,
                JobTitle = "Operations",
                WorkId = workPermitId,
                DepartmentName = "Department6",
                Gender = "Male",
                BadgeNo = "348",
                CompanyName = "Armco",
                CreatedOn = DateTime.Now
            });
            workerList.Add(employees);
            return employees.Count;
        }
        private int LoadWorkers1(int workPermitId)
        {
            List<Employee> employees = new List<Employee>();
            employees.Add(new Employee
            {
                Id = 1,
                FirstName = "Sri",
                MiddleName = "dev",
                LastName = "kumar",
                Name = "Suresh",
                Role = "management",
                IsActive = true,
                ImageUrl = null, // Assuming no image URL is provided
                PhoneNo = 1294487654,
                Email = "suresh@gmail.com",
                DepartmentId = 1, // Assigning a default value or according to your logic
                JobTitle = "Transport",
                WorkId = workPermitId,
                DepartmentName = "Department1",
                Gender = "Male",
                BadgeNo = "343",
                CompanyName = "Armco",
                CreatedOn = DateTime.Now // You may adjust this to the actual creation date
            });

            employees.Add(new Employee
            {
                Id = 2,
                FirstName = "Smt",
                MiddleName = "devi",
                LastName = "nair",
                Name = "Geetha",
                Role = "accountant",
                IsActive = true,
                ImageUrl = null,
                PhoneNo = 1294487656,
                Email = "geetha@gmail.com",
                DepartmentId = 3,
                JobTitle = "Finance",
                WorkId = workPermitId,
                DepartmentName = "Department3",
                Gender = "Female",
                BadgeNo = "345",
                CompanyName = "Armco",
                CreatedOn = DateTime.Now
            });

            employees.Add(new Employee
            {
                Id = 3,
                FirstName = "Sri",
                MiddleName = "tech",
                LastName = "rao",
                Name = "Arjun",
                Role = "developer",
                IsActive = false,
                ImageUrl = null,
                PhoneNo = 1294487657,
                Email = "arjun@gmail.com",
                DepartmentId = 4,
                JobTitle = "IT",
                WorkId = workPermitId,
                DepartmentName = "Department4",
                Gender = "Male",
                BadgeNo = "346",
                CompanyName = "Armco",
                CreatedOn = DateTime.Now
            });

            employees.Add(new Employee
            {
                Id = 4,
                FirstName = "Smt",
                MiddleName = "hr",
                LastName = "reddy",
                Name = "Lakshmi",
                Role = "manager",
                IsActive = true,
                ImageUrl = null,
                PhoneNo = 1294487658,
                Email = "lakshmi@gmail.com",
                DepartmentId = 5,
                JobTitle = "HR",
                WorkId = workPermitId,
                DepartmentName = "Department5",
                Gender = "Female",
                BadgeNo = "347",
                CompanyName = "Armco",
                CreatedOn = DateTime.Now
            });

            employees.Add(new Employee
            {
                Id = 5,
                FirstName = "Sri",
                MiddleName = "op",
                LastName = "singh",
                Name = "Manoj",
                Role = "operator",
                IsActive = true,
                ImageUrl = null,
                PhoneNo = 1294487659,
                Email = "manoj@gmail.com",
                DepartmentId = 6,
                JobTitle = "Operations",
                WorkId = workPermitId,
                DepartmentName = "Department6",
                Gender = "Male",
                BadgeNo = "348",
                CompanyName = "Armco",
                CreatedOn = DateTime.Now
            });
            workerList.Add(employees);
            return employees.Count;
        }

        private void LoadWorkPermit()
        {
            WorkPermit workPermit = new WorkPermit();
            int workerCount = 0;
            List<Worker> workers = new List<Worker>();
            workers.Add(new Worker { EmployeeId = 1222 });
            workers.Add(new Worker { EmployeeId = 1224 });
            workers.Add(new Worker { EmployeeId = 1225 });
            workers.Add(new Worker { EmployeeId = 1226 });
            workers.Add(new Worker { EmployeeId = 1227 });
            workPermit = new WorkPermit
            {
                Id = 232,
                WorkPermitTypeId = 1, // Example ID, adjust as needed
                WorkPermitType = "Cold Work Permit",
                Compliance = "Active",
                ReceiverId = 101, // Example ID, adjust as needed
                IssuerId = 201, // Example ID, adjust as needed
                IssuerName = "Admin",
                ReceiverName = "Android Device 45",
                PlannedStartDate = DateTime.Now,
                Latitude = 5.66,
                Longitude = 945.93,
                Address = "some address",
                DeviceId = 4002,
                DeviceName = "Device 4002",
                LocationId = 10, // Example ID, adjust as needed
                LocationName = "Location 1",
                Workers = workers
            };
            workPermitList.Add(workPermit);
            workerCount = LoadWorkers(workPermit.Id);
            workers = new List<Worker>();
            workers.Add(new Worker { EmployeeId = 1 });
            workers.Add(new Worker { EmployeeId = 2 });
            workers.Add(new Worker { EmployeeId = 3 });
            workers.Add(new Worker { EmployeeId = 4 });
            workers.Add(new Worker { EmployeeId = 5 });
            workPermit = new WorkPermit
            {
                Id = 233,
                WorkPermitTypeId = 2, // Example ID, adjust as needed
                WorkPermitType = "Hot Work Permit",
                Compliance = "Active",
                ReceiverId = 102, // Example ID, adjust as needed
                IssuerId = 202, // Example ID, adjust as needed
                IssuerName = "Admin",
                ReceiverName = "Android Device 46",
                PlannedStartDate = DateTime.Now,
                Latitude = 6.66,
                Longitude = 946.93,
                Address = "another address",
                DeviceId = 4003,
                DeviceName = "Device 4003",
                LocationId = 11, // Example ID, adjust as needed
                LocationName = "Location 2",
                Workers = workers
            };
            workPermitList.Add(workPermit);
            workerCount = LoadWorkers1(workPermit.Id);
        }

        private async void SendWorkPermit()
        {
            int temp = workPermitIndx;
            temp++;
            curntWorkPermitNo = temp.ToString("D2");

            string workPermitJsonStr = JsonSerializer.Serialize(workPermitList[workPermitIndx]);
            workPermitJsonStr = workPermitJsonStr.Replace("\"", "\\\"").Replace("'", "\\'");
            string cmd = $"shell am broadcast -a DataFromDesktop --es DataFromDesktop 'WP{curntWorkPermitNo}_SYNC, {workPermitJsonStr}'";
            runAdbCommand(cmd);
        }

        private async void SendWorker()
        {
            workerIndx = workerIndx + 1;
            curntWorkerNo = workerIndx.ToString("D3");
            string workerJsonStr = JsonSerializer.Serialize(workerList[workPermitIndx][workerIndx - 1]);
            workerJsonStr = workerJsonStr.Replace("\"", "\\\"").Replace("'", "\\'");
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
                string output = adbProcess.StandardOutput.ReadToEnd();
                string error = adbProcess.StandardError.ReadToEnd();
                adbProcess.WaitForExit();
                if (output.Contains(reciever))
                    return true;
            }
            return false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
        }
    }
}
