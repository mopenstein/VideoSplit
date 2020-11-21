using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using DirectShowLib;
using DirectShowLib.DES;
using System.Runtime.InteropServices;
using System.Globalization;

namespace ConsoleApplication1
{
    class Program {

        static List<string> GetFiles(string folder)
        {
            List<string> str = new List<string>();
            int i = 0;
            foreach (string file in Directory.EnumerateFiles(folder, "*.*"))
            {
                string fe = Path.GetExtension(file.ToString());
                if (fe != ".filter" && fe != ".txt" && fe != ".threshold" && fe != ".options") str.Add(file.ToString());
                i++;
            }
            return str;
        }

        static string ffmpeg_location = "C:\\ffmpeg\\ffmpeg.exe";
        static string temp_folder = Path.GetDirectoryName(ffmpeg_location) + "\\temp_folder";

        static void checkCommercials()
        {
            Process proc = new Process();
            proc.StartInfo.FileName = ffmpeg_location;
            string folder = Path.GetDirectoryName(ffmpeg_location) + "\\convert\\";
            string output_folder = Path.GetDirectoryName(ffmpeg_location) + "\\output\\";

            foreach (string file in Directory.EnumerateFiles(folder, "*.*"))
            {
                //File.Move(file.ToString(), folder + Path.GetFileName(file.ToString().Replace(" ", "")));
            }

            //get duration
            string line;

            foreach (string file in Directory.EnumerateFiles(folder, "*.*"))
            {
                //convert commercial

                string new_file = output_folder + Path.GetFileNameWithoutExtension(file.ToString()) + ".mp4";

                if (File.Exists(new_file) == false)
                {
                    string[] temp = getDurationAndAudioFilter(file);
                    Console.WriteLine(temp[1]);
                    proc.StartInfo.Arguments = "-i " + "\"" + file.ToString() + "\"" + "  -bsf:v h264_mp4toannexb -f mpegts " + temp[1] + " -vf scale=640:480 -y -b:v 2M " + "\"" + new_file + "\"";
                    //proc.StartInfo.Arguments = "-i " + file.ToString() + " -c:v libx264 -preset slow -crf 22 -c:a copy " + new_file;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.UseShellExecute = false;
                    if (!proc.Start())
                    {
                        Console.WriteLine("Error starting");
                        return;
                    }
                    StreamReader reader = proc.StandardError;
                    reader = proc.StandardError;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Console.WriteLine(line);
                    }
                    proc.Close();
//                    File.Delete(file.ToString());
                }

            }
        }

        static void checkSplits()
        {
            Process proc = new Process();
            proc.StartInfo.FileName = ffmpeg_location;
            string folder = Path.GetDirectoryName(ffmpeg_location) + "\\split\\";
            string output_folder = Path.GetDirectoryName(ffmpeg_location) + "\\output\\";

            foreach (string file in Directory.EnumerateFiles(folder, "*.*"))
            {
                File.Move(file.ToString(), folder + Path.GetFileName(file.ToString().Replace(" ", "")));
            }

            //get duration
            string line;
            StreamReader reader;
            Random rnd = new Random();

            foreach (string file in Directory.EnumerateFiles(folder, "*.*"))
            {
                if (Path.GetExtension(file).ToLower() != ".txt")
                {
                    //convert 
                    bool auto_split = true;
                    string[] lines = new String[200];
                    if(File.Exists(folder + Path.GetFileNameWithoutExtension(file) + ".txt")==true)
                    {
                        lines = File.ReadAllLines(folder + Path.GetFileNameWithoutExtension(file) + ".txt");
                        auto_split = false;
                    }

                    if (lines[0] == "autodetect" || auto_split == true )
                    {

                        List<TimeSpan> breaks = scanForCommercialBreaks(file, .1);
                        TimeSpan interval = new TimeSpan();

                        string[] temp = getDurationAndAudioFilter(file);
                        string audio_filter = temp[1];
                        breaks.Add(interval);
                        foreach (TimeSpan aa in breaks)
                        {
                            Console.WriteLine(aa.ToString());
                            AddLog(aa.ToString());
                        }
                        int times = breaks.Count - 1;
                        for (int i = 1; i <= times; i++)
                        {
                            AddLog("Autodetected commercial. Splitting video #" + i.ToString());
                            TimeSpan length = breaks[i] - breaks[i - 1];

                            Console.WriteLine("-i " + file + " -bsf:v h264_mp4toannexb -f mpegts " + audio_filter + " -vf scale=640:480 -y -b:v 2M -ss 00:" + breaks[i - 1].Minutes + ":" + breaks[i - 1].Seconds + " -t 00:" + length.Minutes + ":" + length.Seconds + " c:\\ffmpeg\\output\\" + Path.GetFileNameWithoutExtension(file) + "_" + i.ToString()  + ".mpg");
                            //Console.ReadKey();

                            proc.StartInfo.Arguments = "-i " + "\"" + file + "\"" + " -bsf:v h264_mp4toannexb -f mpegts " + audio_filter + " -vf scale=640:480 -y -b:v 2M -ss 00:" + breaks[i - 1].Minutes + ":" + breaks[i - 1].Seconds + " -t 00:" + length.Minutes + ":" + length.Seconds + " c:\\ffmpeg\\output\\" + Path.GetFileNameWithoutExtension(file) + "_" + i.ToString() + "_" + rnd.Next(1, 999).ToString() + ".mpg";
                            AddLog(proc.StartInfo.Arguments.ToString());
                            proc.StartInfo.RedirectStandardError = true;
                            proc.StartInfo.UseShellExecute = false;
                            if (!proc.Start())
                            {
                                Console.WriteLine("Error starting");
                                return;
                            }

                            reader = proc.StandardError;
                            while ((line = reader.ReadLine()) != null)
                            {
                                Console.WriteLine(line);
                            }
                            proc.Close();
                        }
                    }
                    else
                    {
                        TimeSpan start = new TimeSpan();
                        TimeSpan stop = new TimeSpan();
                        TimeSpan diff = new TimeSpan();
                        string[] temp = getDurationAndAudioFilter(file);

                        Console.WriteLine("AudioFilter: " + temp[1]);
                        //Console.ReadKey();

                        for (int z = 1; z < lines.Length; z++)
                        {
                            start = TimeSpan.Parse(lines[z - 1]);
                            stop = TimeSpan.Parse(lines[z]);
                            diff = stop - start;
                            string start_str = "00:" + lines[z - 1];
                            string length_str = "00:" + diff.ToString().Substring(0, diff.ToString().LastIndexOf(":"));



                            proc.StartInfo.Arguments = "-i " + "\"" + file + "\"" + " -bsf:v h264_mp4toannexb -f mpegts " + temp[1] + " -vf scale=640:480 -y -b:v 2M -ss " + start_str + " -t " + length_str + " " + output_folder + Path.GetFileNameWithoutExtension(file).ToString() + rnd.Next(1, 9999).ToString() + ".mpg";
                            proc.StartInfo.RedirectStandardError = true;
                            proc.StartInfo.UseShellExecute = false;
                            if (!proc.Start())
                            {
                                Console.WriteLine("Error starting");
                                return;
                            }
                            reader = proc.StandardError;
                            while ((line = reader.ReadLine()) != null)
                            {
                                Console.WriteLine(line);
                            }
                            proc.Close();

                        }
                    }
                }
            }
        }

        static string[] getDurationAndAudioFilter(string file)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = ffmpeg_location;

            string fname_noext = Path.GetFileNameWithoutExtension(file);
            string fname_root = Path.GetDirectoryName(file);

            string filename = file;
            string audio_filter = "";
            string dur = "";
            string resolution = "";
            AddLog("Getting Duration and Audio Filter string for " + file.ToString());
            //get duration
            proc.StartInfo.Arguments = "-i " + "\"" + filename + "\"" + " -vf \"blackdetect=d=2:pix_th=0.00\" -af volumedetect -f null -";
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            if (!proc.Start())
            {
                Console.WriteLine("Error starting");
            }
            StreamReader reader = proc.StandardError;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int a = line.IndexOf("Duration");
                if (a >= 0)
                {
                    int b = line.IndexOf(".", a + 1);
                    dur = line.Substring(a + 10, b - a - 10);
                    AddLog("Found length "+ dur.ToString());
                    Console.WriteLine();
                    return new string[] { dur, "5" };
                }

                a = line.IndexOf("Stream"); //get resolution of video
                if (a >= 0)
                {
                    string[] parts = line.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                    foreach(string dd in parts) 
                    {
                        if (dd.IndexOf("x") >= 0) resolution = dd;
                    }
                }
                
                a = line.IndexOf("max_volume: ");
                if (a >= 0)
                {
                    int b = line.IndexOf("dB", a + 1);

                    string num = line.Substring(a + 12, b - a - 13);
                    double max_volume = Double.Parse(num, System.Globalization.NumberStyles.Any);
                    if (max_volume >= 0) //too loud
                    {
                        AddLog("Audio is too loud, decreasing by " + Math.Abs(max_volume).ToString() + "dB");
                        double mean = -max_volume;
                        audio_filter = "-af volume=-" + Math.Abs(mean).ToString() + "dB:precision=fixed ";
                    }
                    else
                    {
                        AddLog("Audio is too low, increasing by " + Math.Abs(max_volume).ToString() + "dB");
                        double mean = max_volume;
                        audio_filter = "-af volume=" + Math.Abs(mean).ToString() + "dB:precision=fixed ";
                    }

                    Console.WriteLine(audio_filter);
                }
                Console.WriteLine(line);
            }
            //Console.ReadKey();
            proc.Close();
            AddLog("Filter: " + audio_filter);
            return new string[] { dur, audio_filter };
        }

        static void joinConcat(string concat, string output_name=null)
        {
            emptyTemp();
            Process proc = new Process();
            proc.StartInfo.FileName = ffmpeg_location;
            Random rnd = new Random();
            string file =  "random_commercials_" + rnd.Next(1, 9999).ToString() + ".mpg";
            if(output_name!=null) file = output_name;
            string fname_noext = Path.GetFileNameWithoutExtension(file);
            string fname_root = Path.GetDirectoryName(file);
            string output_folder = Path.GetDirectoryName(ffmpeg_location) + "\\output\\";

            AddLog("CONCAT string generated: " + concat);
            Console.WriteLine("-i \"concat:" + concat + "\" -c copy -f mpegts -analyzeduration 2147483647 -probesize 2147483647 -y -b:v 2M " + output_folder + fname_noext + ".mpg");
            //Console.ReadKey();
            proc.StartInfo.Arguments = "-i \"concat:" + concat + "\" -c copy -f mpegts -analyzeduration 2147483647 -probesize 2147483647 -y -b:v 2M " + output_folder + fname_noext + ".mpg";
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            AddLog("Merging all files and commercials...");
            if (!proc.Start())
            {
                Console.WriteLine("Error starting");
                return;
            }
            StreamReader reader = proc.StandardError;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                Console.WriteLine(line);
            }
            proc.Close();
            AddLog("Merge complete.");
            AddLog("Finished converting " + file.ToString());
            Console.WriteLine("Finished!");
        }

        static string getVideoOption(string file, string option)
        {
            string fname_noext = Path.GetFileNameWithoutExtension(file);
            string fname_root = Path.GetDirectoryName(file);

            if (File.Exists(fname_root + "\\" + fname_noext + ".options"))
            {
                AddLog("Requesting video specific options...");
                string[] lines = File.ReadAllLines(fname_root + "\\" + fname_noext + ".options");
                for (int i = 0; i < lines.Count(); i++)
                {
                    string[] opt = lines[i].Split(new string[] { "=" }, StringSplitOptions.None);
                    if (opt[0].ToLower() == option.ToLower())
                    {
                        return opt[1];
                    }
                }
            }
            return null;
        }

        static void convertShow(string file, List<TimeSpan> breaks)
        {
            emptyTemp();


            Process proc = new Process();
            proc.StartInfo.FileName = ffmpeg_location;

            string filter = "";
            string fname_noext = Path.GetFileNameWithoutExtension(file);
            string fname_root = Path.GetDirectoryName(file);

            string filename = file;

            string output_folder = Path.GetDirectoryName(ffmpeg_location) + "\\output\\";

            string audio_filter = "";
            //get duration
            proc.StartInfo.Arguments = "-i " + "\"" + filename + "\"" + "  -af volumedetect -f null -";
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            if (!proc.Start())
            {
                Console.WriteLine("Error starting");
                return;
            }
            StreamReader reader = proc.StandardError;
            string line;
            TimeSpan interval = new TimeSpan();

            string[] temp = getDurationAndAudioFilter(file);

            interval = TimeSpan.Parse(temp[0]); //duration


            //options for video file
            if (File.Exists(fname_root + "\\" + fname_noext + ".options"))
            {
                AddLog("Found video specific options. Parsing...");
                string[] lines = File.ReadAllLines(fname_root + "\\" + fname_noext + ".options");
                for (int i = 0; i < lines.Count(); i++)
                {
                    string[] opt = lines[i].Split(new string[] { "=" }, StringSplitOptions.None);
                    if (opt[0].ToLower() == "video_filter" && opt[1]!="")
                    {
                        filter = opt[1] + ",";
                        AddLog("Setting video filter: " + filter);
                    }
                    else if (opt[0].ToLower() == "audio_filter" && opt[1] != "")
                    {
                        if (opt[1] == "auto")
                        {
                            audio_filter = " -af volume=" + temp[1] + "dB:precision=fixed ";
                        }
                        else if (opt[1] == "default")
                        {
                            audio_filter = " -af volume=15dB:precision=fixed ";
                        }
                        else
                        {
                            audio_filter = " " + opt[1] + " ";
                        }
                        
                        AddLog("Setting audio filter: " + audio_filter);
                    }
                }
            }

            double times = 0;
            //split file
            if (breaks.Count > 0) //break at set points
            {
                AddLog("Going to split this show into " + (breaks.Count).ToString() + " parts");
                breaks.Add(interval);
                times = breaks.Count - 1;
                for (int i = 1; i <= times; i++)
                {
                    TimeSpan length = breaks[i] - breaks[i - 1];
                    AddLog("Commercial break at " + breaks[i].ToString());
                    AddLog("Length between breaks is " + length.ToString());
                    //Console.ReadKey();

                    proc.StartInfo.Arguments = "-i " + "\"" + filename + "\"" + " -bsf:v h264_mp4toannexb -f mpegts " + audio_filter + " -vf  \"" + filter + "scale=640:480\" -y -b:v 2M -ss " + breaks[i - 1].Hours + ":" + breaks[i - 1].Minutes + ":" + breaks[i - 1].Seconds + " -t 00:" + length.Minutes + ":" + length.Seconds + " " + temp_folder + "\\temp" + i.ToString() + ".mpg";
                    AddLog("Using filter commands: " + proc.StartInfo.Arguments.ToString());
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.UseShellExecute = false;
                    if (!proc.Start())
                    {
                        Console.WriteLine("Error starting");
                        return;
                    }
                    reader = proc.StandardError;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Console.WriteLine(line);
                    }
                    proc.Close();
                }

            }
            else
            {
                double totalMins = interval.TotalMinutes;
                int length_in_minutes = 11;
                times = totalMins / length_in_minutes;
                for (int i = 0; i < times; i++)
                {

                    proc.StartInfo.Arguments = "-i " + "\"" + filename + "\"" + " -bsf:v h264_mp4toannexb -f mpegts " + audio_filter + " -vf  \"" + filter + "scale=640:480\" -y -b:v 2M -ss 00:" + i * length_in_minutes + ":00 -t 00:" + length_in_minutes.ToString() + ":00 " + temp_folder + "\\temp" + i.ToString() + ".mpg";
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.UseShellExecute = false;
                    if (!proc.Start())
                    {
                        Console.WriteLine("Error starting");
                        return;
                    }
                    reader = proc.StandardError;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Console.WriteLine(line);
                    }
                    proc.Close();
                }
            }
            string concat = "";
            AddLog("Generating CONCAT string...");
            for (int i = 1; i <= times; i++)
            {
                if (File.Exists(temp_folder + "\\temp" + i.ToString() + ".mpg"))
                {
                    concat += temp_folder + "\\temp" + i.ToString() + ".mpg|" + (new Foo().getSomeCommercials(Path.GetDirectoryName(ffmpeg_location), 4));
                }
                else
                {
                    Console.WriteLine(temp_folder + "\\temp" + i.ToString() + ".mpg does not exist!");
                    AddLog("Could not find file #" + i.ToString());
                }

            }
            concat = concat.Substring(0, concat.Length - 1);
            AddLog("CONCAT string generated: " + concat);
            Console.WriteLine("-i \"concat:" + concat + "\" -c copy -f mpegts -analyzeduration 2147483647 -probesize 2147483647 -y -b:v 2M " + output_folder + fname_noext + ".mpg");
            
            //Console.ReadKey();
            //proc.StartInfo.Arguments = "-i \"concat:" + concat + "\" -c copy -f mpegts -analyzeduration 2147483647 -probesize 2147483647 -y -b:v 2M " + output_folder + fname_noext + ".mpg";
            proc.StartInfo.Arguments = "-i \"concat:" + concat + "\" -c:v libx264 -preset slow -b:v 800 -crf 28 -c:a copy " + "\"" + output_folder + fname_noext + ".mp4" + "\"";
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            AddLog("Merging all files and commercials...");
            if (!proc.Start())
            {
                Console.WriteLine("Error starting");
                return;
            }
            reader = proc.StandardError;
            while ((line = reader.ReadLine()) != null)
            {
                Console.WriteLine(line);
            }
            proc.Close();
            
            //joinConcat(concat);

            AddLog("Merge complete.");
            AddLog("Finished converting " + file.ToString());
            Console.WriteLine("Finished!");
            return;
        }

        static void emptyTemp()
        {
            string folder = temp_folder;
            List<string> files = GetFiles(folder);
            AddLog("Cleaning up temp folder.");
            foreach (string s in files)
            {
                File.Delete(s);
            }
        }

        static void joinVideos()
        {
            emptyTemp();
            Process proc = new Process();
            proc.StartInfo.FileName = ffmpeg_location;
            AddLog("Checking for files to join...");

            string output_folder = Path.GetDirectoryName(ffmpeg_location) + "\\output\\";
            string folder = Path.GetDirectoryName(ffmpeg_location) + "\\join\\";
            string fname_noext = "";
            List<string> files = GetFiles(folder);
            if (files.Count == 0) return;
            string concat = "";
            string line;
            StreamReader reader;

            AddLog("Renaming join files...");
            foreach (string s in files)
            {
                if (("a" + s).IndexOf(" ") > 0)
                {
                    File.Move(s, s.Replace(" ", "-"));
                }
            }

            files = GetFiles(folder);
            int i = 0;
            files = files.OrderBy(o => o.ToString()).ToList();
            foreach (string s in files)
            {
                if (Path.GetExtension(s).ToLower() != ".mpg")
                {
                    string[] info = getDurationAndAudioFilter(s);
                    AddLog(i.ToString() + ") Converting " + s);
                    proc.StartInfo.Arguments = "-i " + s + " -bsf:v h264_mp4toannexb -f mpegts -vf scale=640:480 -y -b:v 2M " + temp_folder + "\\temp" + i.ToString() + ".mpg";
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.UseShellExecute = false;
                    if (!proc.Start())
                    {
                        Console.WriteLine("Error starting");
                        return;
                    }
                    reader = proc.StandardError;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Console.WriteLine(line);
                    }
                    proc.Close();
                }
                else
                {
                    Console.WriteLine("Copying file #" + i.ToString());
                    AddLog(i.ToString() + ") No need to convert " + s + ", copying instead");
                    File.Copy(s, "" + temp_folder + "\\temp" + i.ToString() + ".mpg");
                }

                i++;

            }
            files = GetFiles(temp_folder);

            AddLog("Generating CONCAT string...");
            foreach (string s in files)
            {
                if (File.Exists(s))
                {
                    fname_noext += Path.GetFileNameWithoutExtension(s) + "_";
                    concat += s + "|";
                }
                else
                {
                    AddLog("Could not find file " + s);
                }

            }
            concat = concat.Substring(0, concat.Length - 1);
            AddLog("CONCAT string generated: " + concat);
            Console.WriteLine("-i \"concat:" + concat + "\" -c copy -f mpegts -analyzeduration 2147483647 -probesize 2147483647 -y -b:v 2M " + output_folder + "joined_" + i.ToString() + "_files.mpg");
            //Console.ReadKey();
            proc.StartInfo.Arguments = "-i \"concat:" + concat + "\" -c copy -f mpegts -analyzeduration 2147483647 -probesize 2147483647 -y -b:v 2M " + "\"" + output_folder + "joined_" + i.ToString() + "_files.mpg" + "\"";
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            AddLog("Merging all files and commercials...");
            if (!proc.Start())
            {
                Console.WriteLine("Error starting");
                return;
            }
            reader = proc.StandardError;
            while ((line = reader.ReadLine()) != null)
            {
                Console.WriteLine(line);
            }
            proc.Close();
            Console.WriteLine("Cleaning up temp files...");
            AddLog("Merge complete. Cleaning up temp files...");
            AddLog("Finished converting " + fname_noext.ToString());
            Console.WriteLine("Finished!");
            return;

        }

        static List<TimeSpan> scanForCommercialBreaks(string file, double threshhold)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = ffmpeg_location;

            string fname_noext = Path.GetFileNameWithoutExtension(file);
            string fname_root = Path.GetDirectoryName(file);

            string filename = file;

            //get duration
            proc.StartInfo.Arguments = "-i " + "\"" + filename + "\"" + " -vf blackframe -an -f null -";
            //proc.StartInfo.Arguments = "-i " + "\"" + filename + "\"" + " -vf select='gte(scene,0)' -an -f null -";

            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            if (!proc.Start())
            {
                Console.WriteLine("Error starting");
            }
            StreamReader reader = proc.StandardError;
            List<TimeSpan> nums = new List<TimeSpan>();
            string line;
            string last_num = "";
            while ((line = reader.ReadLine()) != null)
            {
                int a = line.IndexOf("Parsed_blackframe");
                AddLog(line.ToString());
                if (a >= 0)
                {
                    a = line.IndexOf(" t:");
                    int b = line.IndexOf(" ", a + 1);
                    string num = line.Substring(a + 3, b - a - 3);
                    if (last_num != "")
                    {
                        if (Double.Parse(num) - Double.Parse(last_num) > 2) nums.Add(TimeSpan.FromSeconds(0));
                    }
                    last_num = num;
                    nums.Add(TimeSpan.FromSeconds(Double.Parse(num)));
                    Console.WriteLine(TimeSpan.FromSeconds(Double.Parse(num)));
                }
                else
                {
                    if (nums.Count > 0)
                    {
                        if (nums[nums.Count - 1].TotalSeconds != 0)
                        {
                            nums.Add(TimeSpan.FromSeconds(0));
                        }
                    }
                    Console.WriteLine(line);
                }
            }
            Console.WriteLine("Scanning for Commercial Breaks...");
            for (int i = 1; i < nums.Count; i++)
            {
                if (nums[i - 1].TotalSeconds == 0 && nums[i].TotalSeconds == 0)
                {
                    nums.RemoveAt(i);
                }
            }
            List<TimeSpan> commerical_breaks = new List<TimeSpan>();
            commerical_breaks.Add(TimeSpan.FromSeconds(0));
            int last = 0;
            for(int i = 0;i<nums.Count;i++) 
            {
                TimeSpan t = nums[i];
                Console.WriteLine(i + " - " + t.ToString());
                if (t.TotalSeconds == 0) //start new count
                {
                    if (last != 0)
                    {

                        TimeSpan q = nums[last+1];
                        TimeSpan qa = nums[i-1];
                        Console.WriteLine("Total: " + (qa - q).TotalSeconds + " - " + qa.ToString() + " - " + q.ToString());
                        AddLog("Total: " + (qa - q).TotalSeconds + " - " + qa.ToString() + " - " + q.ToString());
                        if ((qa - q).TotalSeconds > threshhold)
                        {

                            Console.WriteLine("Found commercial break at " + q.ToString());
                            AddLog("Found commercial break at " + q.ToString());
                            //Console.ReadKey();
                            q = q + TimeSpan.FromSeconds(1);
                            commerical_breaks.Add(q);
                        }
                        last = i;
                    }
                    else
                    {
                        last = i;
                    }
                }
                
                //Console.WriteLine(t.ToString());
            }
            Console.WriteLine("Commercial breaks found: " + commerical_breaks.Count);
            AddLog("Commercial breaks found: " + commerical_breaks.Count);
            //Console.ReadKey();
            proc.Close();
            TimeSpan lt = new TimeSpan(0, 0, 0);
            List<TimeSpan> new_com = new List<TimeSpan>();
            foreach (TimeSpan t in commerical_breaks)
            {
                if (t.TotalMinutes - lt.TotalMinutes > 5 || lt.TotalMinutes == 0)
                {
                    new_com.Add(t);
                    AddLog("Found commercial break at " + t.ToString());
                }

                lt = t;
            }

            return new_com;
        }

        private static string Log_File = Path.GetDirectoryName(ffmpeg_location) + "\\output\\log.txt";

        static void ResetLog()
        {
            if (File.Exists(Log_File) == false) File.Create(Log_File).Close();
            File.WriteAllText(Log_File, string.Empty);
        }

        static void AddLog(string log)
        {
            File.AppendAllText(Log_File, DateTime.Now.ToString() + "    " +  log + "\r\n");
        }

        static List<TimeSpan> breakz = new List<TimeSpan>();

        static void Main(string[] args)
        {

            string app_path = AppDomain.CurrentDomain.BaseDirectory.ToString();
            if(File.Exists(app_path + "\\options.txt"))
            {
                string[] lines = File.ReadAllLines(app_path + "\\options.txt");
                for (int i = 0; i < lines.Count(); i++)
                {
                    string[] opt = lines[i].Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                    if (opt[0].ToLower() == "ffmpeg location")
                    {
                        Console.WriteLine("Setting FFMPEG location to " + opt[1]);
                        ffmpeg_location = opt[1];
                    }
                }
            }
            ResetLog();
            string folder = Path.GetDirectoryName(ffmpeg_location);

            if (Directory.Exists(folder + "\\shows\\") == false) Directory.CreateDirectory(folder + "\\shows\\");
            List<string> shows = GetFiles(folder + "\\shows\\");
            if (Directory.Exists(folder + "\\shows\\") == false) Directory.CreateDirectory(folder + "\\output\\");
            List<string> finished_shows = GetFiles(folder + "\\output\\");
            if (Directory.Exists(folder + "\\shows\\") == false) Directory.CreateDirectory(folder + "\\convert\\");
            List<string> commercials = GetFiles(folder + "\\convert\\");
            if (Directory.Exists(folder + "\\shows\\") == false) Directory.CreateDirectory(folder + "\\bopen\\");
            List<string> bumpers_open = GetFiles(folder + "\\bopen\\");
            if (Directory.Exists(folder + "\\shows\\") == false) Directory.CreateDirectory(folder + "\\bclose\\");
            List<string> bumpers_close = GetFiles(folder + "\\bclose\\");

            //check and convert any non-mpg video to mpg
            Console.WriteLine(" ___  ___  __    __  ____    _  _  __  ___   ___   __  ");
            Console.WriteLine("/ __)(  ,\\(  )  (  )(_  _)  ( )( )(  )(   \\ (  _) /  \\ ");
            Console.WriteLine("\\__ \\ ) _/ )(__  )(   )(     \\\\//  )(  ) ) ) ) _)( () )");
            Console.WriteLine("(___/(_)  (____)(__) (__)    (__) (__)(___/ (___) \\__/ ");


            if (File.Exists(folder + "\\rnd.txt")) //create 15 random commercial videos
            {
                Console.WriteLine("Nice! Gonna make some randon commercial blocks...");
                Console.ReadKey();
                for (int i = 0; i < 15; i++)
                {
                    string tmp_comm = new Foo().getSomeCommercials(Path.GetDirectoryName(ffmpeg_location), 5);
                    Console.WriteLine("Got the commercials...");
                    joinConcat(tmp_comm);
                    Console.WriteLine("Random Commercials saved!");
                }
                File.Delete(folder + "\\rnd.txt"); //delete it so we don't keep creating rnd videos.
                Console.ReadKey();
            }


            Console.WriteLine("Checking " + commercials.Count + " conversion file(s)....");
            AddLog("Checking to see if any files need to be converted to MPEG2");
            checkCommercials();
            Console.WriteLine("Files have been checked! Press any Key to continue....");
            AddLog("Conversions have been checked.");
            Console.ReadKey();
            Console.WriteLine("Checking for video to split....");
            AddLog("Checking to see if any videos need to be split.");
            checkSplits();
            Console.WriteLine("Splits have been checked! Press any Key to continue....");
            AddLog("Splits have been checked.");
            Console.ReadKey();

            Console.WriteLine("Checking for video to join....");
            AddLog("Checking to see if any videos need to be joined.");
            joinVideos();
            Console.WriteLine("Joins have been checked! Press any Key to continue....");
            AddLog("Joins have been checked.");
            Console.ReadKey();
            
            if (shows.Count > 0)
            {
                
                AddLog("Begin convert of " + shows[0].ToString());
                AddLog("Scanning show for commercial breaks.");
                breakz.Add(TimeSpan.Parse("0"));

                string ocomms = getVideoOption(shows[0].ToString(), "commercials");
                AddLog("Checking for predefined commercials " + shows[0].ToString());

                if (ocomms!=null || File.Exists(folder + "\\shows\\shared_breaks.txt"))
                {
                    string[] lines;
                    if (ocomms != null)
                    {
                        lines = ocomms.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    else
                    {
                        string fname = folder + "\\shows\\" + "shared_breaks.txt";
                        Console.WriteLine("Ohhhh! Manually adding SHAREDcommercial breaks? Excellent...");
                        lines = File.ReadAllLines(fname);
                    }

                    for (int i = 0; i < lines.Count(); i++)
                    {
                        Console.WriteLine("Found commercial break at: " + lines[i]);
                        breakz.Add(TimeSpan.Parse(lines[i]));
                    }
                }
                else
                {
                    double thresh = .5;
                    string othresh = getVideoOption(shows[0].ToString(), "threshold");
                    AddLog("Checking for threshold " + shows[0].ToString());
                    if (othresh!=null)
                    {
                        thresh = Convert.ToDouble(othresh);
                        AddLog("Threshold found: " + thresh.ToString());
                    }
                    breakz = scanForCommercialBreaks(shows[0].ToString(), thresh);
                }

                Console.WriteLine("Found all the breaks, press any key...");
                Console.ReadKey();

                AddLog("Commercial breaks scan complete. Found " + breakz.Count.ToString() + " commercial breaks.");
                Console.WriteLine(breakz.Count.ToString());
                //Console.ReadKey();
                AddLog("Actual show conversion is beginning...");
                convertShow(shows[0].ToString(), breakz);
                AddLog("Show has been converted.");
            }
            else
            { 
                AddLog("No shows to convert.");
            }

            //convertShow(folder + "comm\\80sKidsCommercials-1987-volume16685.mpg");

            //-vf "blackdetect=d=2:pix_th=0.00"
            emptyTemp();
            Console.WriteLine("All done! Press a key.");
            Console.ReadKey();
            AddLog("END LOG");
            Environment.Exit(0);
            return;
        }
    }
/*split shows based on file time
            if (File.Exists(fname_root + fname_noext + ".txt"))
            {
                
                                    TimeSpan start = new TimeSpan();
                TimeSpan stop = new TimeSpan();
                TimeSpan diff = new TimeSpan();
                string[] lines = File.ReadAllLines(fname_root + fname_noext + ".txt");
                times = lines.Count();
                for (int z = 1; z < times; z++)
                {

                    start = TimeSpan.Parse(lines[z - 1]);
                    stop = TimeSpan.Parse(lines[z]);
                    diff = stop - start;
                    string start_str = "00:" + lines[z - 1];
                    string length_str = "00:" + diff.ToString().Substring(0, diff.ToString().LastIndexOf(":"));

                    proc.StartInfo.Arguments = "-i " + filename + " -bsf:v h264_mp4toannexb -f mpegts " + audio_filter + " -vf scale=640:480 -y -b:v 2M -ss-ss " + start_str + " -t " + length_str + " c:\\ffmpeg\\temp\\temp" + (z-1).ToString() + ".mpg";
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.UseShellExecute = false;
                    if (!proc.Start())
                    {
                        Console.WriteLine("Error starting");
                        return;
                    }
                    reader = proc.StandardError;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Console.WriteLine(line);
                    }
                    proc.Close();
                }
            }
            else 
 */
    public class Foo
    {
        private static readonly Random random = new Random();
        private static readonly object syncLock = new object();
        public static int RandomNumber(int min, int max)
        {
            lock(syncLock) { // synchronize
                return random.Next(min, max);
            }
        }

        public string getSomeCommercials(string folder, int cnt = 3)
        {
            List<string> commercials = GetFiles(folder + "\\comm\\");
            List<string> bumpers_open = GetFiles(folder + "\\bopen\\");
            List<string> bumpers_close = GetFiles(folder + "\\bclose\\");

            string str = "";

            string f = null;
            int r = 0;

            if (bumpers_open.Count <= 0)
            {
                f = "";
            }
            else
            {
                r = RandomNumber(0, bumpers_open.Count - 1);
                f = (string)bumpers_open[r] + "|";
            }
            
            str += f ;
            for (var i = 0; i < cnt; i++)
            {
                r = RandomNumber(0, commercials.Count - 1);
                f = (string)commercials[r];
                str += f + "|";
            }

            if (bumpers_open.Count > 0)
            {
                r = RandomNumber(0, bumpers_close.Count - 1);
                str += (string)bumpers_close[r] + "|";
            }

            return str;
        }

        public List<string> GetFiles(string folder)
        {
            List<string> str = new List<string>();
            int i = 0;
            foreach (string file in Directory.EnumerateFiles(folder, "*.*"))
            {
                str.Add(file.ToString());
                i++;
            }
            return str;
        }
    }

}
