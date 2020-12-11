using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace ConsoleApplication1
{
    class Program {

        static string[] video_file_extensions = new string[] { ".3g2", ".3gp", ".asf", ".avi", ".flv", ".h264", ".m2t", ".m2ts", ".m4v", ".mkv", ".mod", ".mov", ".mp4", ".mpg", ".png", ".tod", ".vob", ".webm", ".wmv" };

        static List<string> GetFiles(string folder, string[] ext_filter = null, string[] name_filter = null)
        {
            if (!Directory.Exists(folder)) return null;
            if(folder=="") return new List<string>();
            List<string> str = new List<string>();
            int i = 0;
            foreach (string file in Directory.EnumerateFiles(folder, "*.*"))
            {
                string fe = Path.GetExtension(file.ToString());
                bool add = false;
                //only include extensions that match ext_filter
                
                if (ext_filter != null)
                {
                    foreach (string f in ext_filter)
                    {
                        if (fe.ToString().ToLower() == f.ToLower()) add = true;
                    }
                }

                //filter file names that meet name_filter
                if (add)
                {
                    string fn = Path.GetFileNameWithoutExtension(file.ToString());
                    if (name_filter != null)
                    {
                        foreach (string f in name_filter)
                        {
                            if (fn.ToString().IndexOf(f) > -1) add = false;
                        }
                    }
                }
                if (add) str.Add(file.ToString());

                i++;
            }
            return str;
        }

        static string ffmpeg_location = "";
        static string ffmpegX_location = "";
        static string temp_folder = "";

        static TimeSpan[] NormalizeAudio(string file)
        {

            TimeSpan[] ret = { TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0) };

            string pad = "_NA_";
            string path = Path.GetDirectoryName(file);
            string file_name = Path.GetFileNameWithoutExtension(file);
            string ext_name = Path.GetExtension(file);

            if (File.Exists(path + "\\" + file_name + pad + "" + ext_name) || file.IndexOf(pad)>=0)
            {
                AddLog("File Exists, skipping normaliztion");
                return ret;
            }

            Process proc = new Process();
            proc.StartInfo.FileName = ffmpegX_location;

            proc.StartInfo.Arguments = "-y -i \"" + file + "\" -af loudnorm=I=-16:TP=-1.5:LRA=11:print_format=json -f null /dev/null";
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;

            if (!proc.Start())
            {
                Console.WriteLine("Error starting");
            }
            StreamReader reader = proc.StandardError;
            string line;
            string input_i = "";
            string input_lra = "";
            string input_tp = "";
            string input_thresh = "";
            string input_offset = "";

            Console.WriteLine("Generating Filter...");
            Console.CursorVisible = false;

            TimeSpan procTDur = new TimeSpan();
            DateTime start = DateTime.Now;

            while ((line = reader.ReadLine()) != null)
            {
                string dur = "";
                int a = line.IndexOf("Duration");
                int x = line.IndexOf("Segment");
                if (a >= 0 && x==-1)
                {
                    int b = line.IndexOf(",", a + 1);
                    dur = line.Substring(a + 10, b - a - 10);
                    procTDur = TimeSpan.Parse(dur);
                }

                int percent = 0;

                try
                {
                    string find = "time=";
                    a = line.IndexOf(find);
                    if (a >= 0)
                    {
                        int b = line.IndexOf(" ", a + 1);
                        dur = line.Substring(a + find.Length, b - a - find.Length);
                        percent = (int)((TimeSpan.Parse(dur).TotalSeconds / procTDur.TotalSeconds) * 100);
                    }
                }
                catch
                { }

                string pbar = "";
                for(int t=0;t<50;t++)
                {
                    if(t<=Math.Floor((double)percent/2))
                    {
                        pbar += "█";
                    }
                    else
                    {
                        pbar += "░";
                    }
                }


                Console.Write(pbar);
                Console.WriteLine();

                try
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                }
                catch { }

                a = 0;
                string phrase = "";

                phrase = "\"input_i\" : \"";
                a = line.IndexOf(phrase);
                x = line.IndexOf("Segment");
                if (a >= 0 && x == -1)
                {
                    int b = line.IndexOf("\"", a + phrase.Length + 1);
                    input_i = line.Substring(a + phrase.Length, b - a - phrase.Length);
                    AddLog("Found input_i: " + line.Substring(a + phrase.Length, b - a - phrase.Length));
                    //Console.WriteLine("Found input_i: " + line.Substring(a + phrase.Length, b - a - phrase.Length));
                    
                }

                phrase = "\"input_lra\" : \"";
                a = line.IndexOf(phrase);
                if (a >= 0)
                {
                    int b = line.IndexOf("\"", a + phrase.Length + 1);
                    input_lra = line.Substring(a + phrase.Length, b - a - phrase.Length);
                    AddLog("Found input_lra: " + line.Substring(a + phrase.Length, b - a - phrase.Length));
                    //Console.WriteLine("Found input_lra: " + line.Substring(a + phrase.Length, b - a - phrase.Length));
                }

                phrase = "\"input_tp\" : \"";
                a = line.IndexOf(phrase);
                if (a >= 0)
                {
                    int b = line.IndexOf("\"", a + phrase.Length + 1);
                    input_tp = line.Substring(a + phrase.Length, b - a - phrase.Length);
                    AddLog("Found input_tp: " + line.Substring(a + phrase.Length, b - a - phrase.Length));
                    //Console.WriteLine("Found input_tp: " + line.Substring(a + phrase.Length, b - a - phrase.Length));
                }

                phrase = "\"input_thresh\" : \"";
                a = line.IndexOf(phrase);
                if (a >= 0)
                {
                    int b = line.IndexOf("\"", a + phrase.Length + 1);
                    input_thresh = line.Substring(a + phrase.Length, b - a - phrase.Length);
                    AddLog("Found input_thresh: " + line.Substring(a + phrase.Length, b - a - phrase.Length));
                    //Console.WriteLine("Found input_thresh: " + line.Substring(a + phrase.Length, b - a - phrase.Length));
                }

                phrase = "\"target_offset\" : \"";
                a = line.IndexOf(phrase);
                if (a >= 0)
                {
                    int b = line.IndexOf("\"", a + phrase.Length + 1);
                    input_offset = line.Substring(a + phrase.Length, b - a - phrase.Length);
                    AddLog("Found input_offset: " + line.Substring(a + phrase.Length, b - a - phrase.Length));
                    //Console.WriteLine("Found input_offset: " + line.Substring(a + phrase.Length, b - a - phrase.Length));
                }
            }

            //ffmpeg -i in.wav -af loudnorm=I=-16:TP=-1.5:LRA=11:
            //measured_I =-27.61:
            //measured_LRA =18.06:
            //measured_TP=-4.47:
            //measured_thresh=-39.20:
            //offset=0.58:linear=true:print_format=summary -ar 48k out.wav
            //
            proc.Close();

            

            string filter = "loudnorm=I=-16:TP=-1.5:LRA=11:measured_I=" + input_i + ":measured_LRA=" + input_lra + ":measured_TP=" + input_tp + ":measured_thresh=" + input_thresh + ":offset=" + input_offset + ":linear=true:print_format=summary";

            proc.StartInfo.FileName = ffmpegX_location;

            proc.StartInfo.Arguments = "-y -i \"" + file + "\" -vcodec copy -af " + filter + " \"" + path + "\\" + file_name + pad + "" + ext_name + "\"";
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;

            if (!proc.Start())
            {
                Console.WriteLine("Error starting");
            }
            reader = proc.StandardError;

            try
            {
                Console.SetCursorPosition(0, Console.CursorTop + 2);
            } catch { }

            TimeSpan total = TimeSpan.FromTicks(DateTime.Now.Ticks - start.Ticks);

            ret[0] = total;
            Console.WriteLine("Finished Generating Filter. Took " + total.ToString());

            Console.WriteLine();

            Console.WriteLine("Applying audio filter...");
            start = DateTime.Now;
            while ((line = reader.ReadLine()) != null)
            {


                string dur = "";
                int a = line.IndexOf("Duration");
                int x = line.IndexOf("Segment");
                if (a >= 0 && x == -1)
                {
                    int b = line.IndexOf(",", a + 1);
                    dur = line.Substring(a + 10, b - a - 10);
                    procTDur = TimeSpan.Parse(dur);
                }

                int percent = 0;

                try
                {
                    string find = "time=";
                    a = line.IndexOf(find);
                    if (a >= 0)
                    {
                        int b = line.IndexOf(" ", a + 1);
                        dur = line.Substring(a + find.Length, b - a - find.Length);
                        percent = (int)((TimeSpan.Parse(dur).TotalSeconds / procTDur.TotalSeconds) * 100);
                    }
                }
                catch
                { }

                string pbar = "";
                for (int t = 0; t < 50; t++)
                {
                    if (t <= Math.Floor((double)percent / 2))
                    {
                        pbar += "█";
                    }
                    else
                    {
                        pbar += "░";
                    }
                }


                Console.Write(pbar);
                Console.WriteLine();

                try
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                }
                catch { }

            }


            try
            {
                Console.SetCursorPosition(0, Console.CursorTop + 1);
            }
            catch { }
            

            total = TimeSpan.FromTicks(DateTime.Now.Ticks - start.Ticks);
            Console.WriteLine();
            ret[1] = total;
            Console.WriteLine("Finished Applying Filter. Took " + total.ToString());

            Console.WriteLine();

            if (File.Exists(file + ".commercials"))
            {
                Console.WriteLine();
                Console.WriteLine("Renamed commercials file!");
                File.SetAttributes(file + ".commercials", FileAttributes.Normal);
                File.Move(file + ".commercials", path + "\\" + file_name + pad + "" + ext_name + ".commercials");
            }

            //make sure the current file and the audio adjusted file exists
            if (File.Exists(file) && File.Exists(path + "\\" + file_name + pad + "" + ext_name))
            {
                Console.WriteLine();
                Console.WriteLine("Deleted original file!");
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }


            Console.WriteLine();
            proc.Close();
            Console.CursorVisible = true;
            return ret;

        }

        static void checkCommercials()
        {
            Process proc = new Process();
            proc.StartInfo.FileName = ffmpeg_location;
            string folder = Path.GetDirectoryName(ffmpeg_location) + "\\convert\\";
            string output_folder = Path.GetDirectoryName(ffmpeg_location) + "\\output\\";

            //foreach (string file in Directory.EnumerateFiles(folder, "*.*"))
            //{
                //File.Move(file.ToString(), folder + Path.GetFileName(file.ToString().Replace(" ", "")));
            //}

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
                  //proc.StartInfo.Arguments = "-i " + "\"" + filename + "\"" + " -bsf:v h264_mp4toannexb -f mpegts " + audio_filter + " -vf  \"" + filter + "scale=640:480\" -r 29.97 -y -b:v 2M -ss " + breaks[i - 1].Hours + ":" + breaks[i - 1].Minutes + ":" + breaks[i - 1].Seconds + " -t 00:" + length.Minutes + ":" + length.Seconds + " " + temp_folder + "\\temp" + i.ToString() + ".mpg";
                    //proc.StartInfo.Arguments = "-i " + "\"" + file.ToString() + "\"" + "  -bsf:v h264_mp4toannexb -f mpegts " + temp[1] + " -vf scale=640:480 -r 29.97 -y -b:v 2M " + "\"" + new_file + "\"";
                    proc.StartInfo.Arguments = "-i \"" + file.ToString() + "\" -vcodec libx264 -crf 23 -s 640x480 -aspect 640:480 -r 29.97 -threads 4 -acodec libvo_aacenc -ab 128k -ar 32000 -async 32000 -ac 2 -scodec copy \"" + new_file + "\"";
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

        static void checkSplits(string in_path, string out_path)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = ffmpeg_location;
            string folder = in_path + "\\";
            string output_folder = out_path;

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
                    /*
                    string[] lines = { };
                    if(File.Exists(folder + Path.GetFileNameWithoutExtension(file) + ".txt")==true)
                    {
                        lines = File.ReadAllLines(folder + Path.GetFileNameWithoutExtension(file) + ".txt");
                        auto_split = false;
                    }

                    if (lines[0] == "autodetect" || auto_split == true )
                    */
                    if (auto_split == true)
                    {

                        List<TimeSpan> breaks = scanForCommercialBreaks(file, 0.3, 0, 0);

                        foreach(TimeSpan tss in breaks)
                        {
                            Console.WriteLine(tss.ToString());
                        }

                        //Console.ReadKey();

                        TimeSpan interval = new TimeSpan();

                        string[] temp = getDurationAndAudioFilter(file);
                        string audio_filter = temp[1];
                        AddLog("audio filter: " + audio_filter);
                        breaks.Add(interval);
                        foreach (TimeSpan aa in breaks)
                        {
                            Console.WriteLine(aa.ToString());
                            AddLog("breaks: " + aa.ToString());
                        }
                        int times = breaks.Count - 1;
                        for (int i = 1; i <= times; i++)
                        {
                            AddLog("Autodetected commercial. Splitting video #" + i.ToString());
                            TimeSpan length = breaks[i] - breaks[i - 1];

                            proc.StartInfo.Arguments = "-i " + "\"" + file + "\" -c:v libx264 -r 29.97 -preset slow -b:v 800 -crf 29.97 -vf \"scale=640:480,setdar=4:3\" -ss " + breaks[i - 1].Hours.ToString().PadLeft(2, '0') + ":" + breaks[i - 1].Minutes.ToString().PadLeft(2, '0') + ":" + breaks[i - 1].Seconds.ToString().PadLeft(2, '0') + "." + breaks[i - 1].Milliseconds + " -t " + length.Hours.ToString().PadLeft(2, '0') + ":" + length.Minutes.ToString().PadLeft(2, '0') + ":" + length.Seconds.ToString().PadLeft(2, '0') + "." + length.Milliseconds + " \"" + output_folder + Path.GetFileNameWithoutExtension(file) + "_" + i.ToString() + "_" + rnd.Next(1, 999).ToString() + ".mp4\"";
                            Console.WriteLine(proc.StartInfo.Arguments);

                            AddLog("Split command:" + proc.StartInfo.Arguments.ToString());
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
                    /*
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



                            proc.StartInfo.Arguments = "-i " + "\"" + file + "\"" + " -bsf:v h264_mp4toannexb -f mpegts " + temp[1] + " -vf scale=640:480 -r 29.97 -y -b:v 2M -ss " + start_str + " -t " + length_str + " " + output_folder + Path.GetFileNameWithoutExtension(file).ToString() + rnd.Next(1, 9999).ToString() + ".mpg";
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
                    */
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
                    //return new string[] { dur, "5" };
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
                        audio_filter = "-af \"volume=-" + Math.Abs(mean).ToString() + "dB\" ";
                    }
                    else
                    {
                        AddLog("Audio is too low, increasing by " + Math.Abs(max_volume).ToString() + "dB");
                        double mean = max_volume;
                        audio_filter = "-af \"volume=" + Math.Abs(mean).ToString() + "dB\" ";
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
            //Console.WriteLine("-i \"concat:" + concat + "\" -c copy -f mpegts -analyzeduration 2147483647 -probesize 2147483647 -y -b:v 2M " + output_folder + fname_noext + ".mpg");
            //Console.ReadKey();
            //proc.StartInfo.Arguments = "-i \"concat:" + concat + "\" -c copy -f mpegts -analyzeduration 2147483647 -probesize 2147483647 -y -b:v 2M " + output_folder + fname_noext + ".mpg";
            //proc.StartInfo.Arguments = "-i \"concat:" + concat + "\" -c:v libx264 -r 29.97 -preset slow -b:v 800 -crf 28 -c:a copy -vf \"scale=640:480,setdar=4:3\" " + "\"" + output_folder + fname_noext + ".mp4" + "\"";
            //proc.StartInfo.Arguments = concat + " -c:v libx264 -r 29.97 -preset slow -b:v 800 -crf 28 -c:a copy -vf \"scale=640:480,setdar=4:3\" " + "\"" + output_folder + fname_noext + ".mp4" + "\"";
            Console.Write("SSSS" + concat);
            //Console.ReadKey();
            File.WriteAllText("mylist.txt", concat);
            Console.Write("AAAAAAAA" + File.ReadAllText("mylist.txt"));
            //Console.ReadKey();
            proc.StartInfo.Arguments = "-f concat -safe 0 -i mylist.txt -c:v libx264 -r 29.97 -preset slow -b:v 800 -crf 28 -c:a copy -vf \"scale=640:480,setdar=4:3\" " + "\"" + output_folder + fname_noext + ".mp4" + "\"";
            

             AddLog(proc.StartInfo.Arguments);
            Console.Write(proc.StartInfo.Arguments);
            //Console.ReadKey();
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
            audio_filter = temp[1];
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
            proc.Close();
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

                    proc.StartInfo.Arguments = "-i " + "\"" + filename + "\"" + " -bsf:v h264_mp4toannexb -f mpegts " + audio_filter + " -vf  \"" + filter + "scale=640:480\" -r 29.97 -y -b:v 2M -ss " + breaks[i - 1].Hours + ":" + breaks[i - 1].Minutes + ":" + breaks[i - 1].Seconds + " -t 00:" + length.Minutes + ":" + length.Seconds + " " + temp_folder + "\\temp" + i.ToString() + ".mpg";
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

                    proc.StartInfo.Arguments = "-i " + "\"" + filename + "\"" + " -bsf:v h264_mp4toannexb -f mpegts " + audio_filter + " -vf  \"" + filter + "scale=640:480\" -r 29.97 -y -b:v 2M -ss 00:" + i * length_in_minutes + ":00 -t 00:" + length_in_minutes.ToString() + ":00 " + temp_folder + "\\temp" + i.ToString() + ".mpg";
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
            proc.StartInfo.Arguments = "-i \"concat:" + concat + "\" -c:v libx264 -r 29.97 -preset slow -b:v 800 -crf 28 -c:a copy " + "\"" + output_folder + fname_noext + ".mp4" + "\"";
            AddLog(proc.StartInfo.Arguments.ToString());
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

            /*
            AddLog("Renaming join files...");
            foreach (string s in files)
            {
                if (("a" + s).IndexOf(" ") > 0)
                {
                    File.Move(s, s.Replace(" ", "-"));
                }
            }
            */
            files = GetFiles(folder);

            /*

            int i = 0;
            files = files.OrderBy(o => o.ToString()).ToList();
            foreach (string s in files)
            {
                if (Path.GetExtension(s).ToLower() != ".mpg")
                {
                    string[] info = getDurationAndAudioFilter(s);
                    AddLog(i.ToString() + ") Converting " + s);
                    proc.StartInfo.Arguments = "-i " + s + " -bsf:v h264_mp4toannexb -f mpegts -vf scale=640:480 -r 29.97 -y -b:v 2M " + temp_folder + "\\temp" + i.ToString() + ".mpg";
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
            */

            int i = 0;
            foreach (string s in files)
            {
                if (File.Exists(s))
                {
                    concat += "file " + s.Replace("\\", "\\\\") + "\r\n";
                    i++;
                }
                else
                {
                    AddLog("Could not find file " + s);
                }

            }

            File.WriteAllText(output_folder + "files.txt", concat);

            while(File.Exists(output_folder + "files.txt")==false)
            {
                Console.WriteLine("Waiting for file to write...");
            }


            if(File.Exists(output_folder + "files.txt")== false)
            {
                Console.WriteLine("File no exists!!!!!!!!!!!!!!!!!!");
            }

            concat = concat.Substring(0, concat.Length - 1);
            AddLog("CONCAT string generated: " + concat);
            Console.WriteLine("-i \"concat:" + concat + "\" -c copy -f mpegts -analyzeduration 2147483647 -probesize 2147483647 -y -b:v 2M " + output_folder + "joined_" + i.ToString() + "_files.mpg");
            //Console.ReadKey();
            //proc.StartInfo.Arguments = "-i \"concat:" + concat + "\" -c copy -f mpegts -analyzeduration 2147483647 -probesize 2147483647 -y -b:v 2M " + "\"" + output_folder + "joined_" + i.ToString() + "_files.mpg" + "\"";
            proc.StartInfo.Arguments = "-f concat -i \"" + output_folder + "files.txt\" -c copy \"" + output_folder + "joined_" + i.ToString() + "_files.mp4" + "\"";

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

        static List<TimeSpan> scanForCommercialBreaks(string file, double threshhold, double wait, int min_time_add=300)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = ffmpeg_location;

            string fname_noext = Path.GetFileNameWithoutExtension(file);
            string fname_root = Path.GetDirectoryName(file);

            string filename = file;

            //get duration
            proc.StartInfo.Arguments = "-i " + "\"" + filename + "\"" + " -vf blackframe -an -f null -";
            AddLog("scanForCommercialBreaks:" + proc.StartInfo.Arguments);
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
            Console.WriteLine("Scanning for Commercial Breaks...");
            DateTime now = DateTime.Now;
            while ((line = reader.ReadLine()) != null)
            {
                if((DateTime.Now - now) > TimeSpan.FromSeconds(.5))
                {
                    Console.Write("_");
                    now = DateTime.Now;
                }
                int a = line.IndexOf("Parsed_blackframe");
                if (a >= 0)
                {
                    a = line.IndexOf(" t:");
                    int b = line.IndexOf(" ", a + 1);
                    string num = line.Substring(a + 3, b - a - 3);
                    if (last_num != "")
                    {
                        //
                        if (Double.Parse(num) - Double.Parse(last_num) > 2) nums.Add(TimeSpan.FromSeconds(0));
                    }
                    last_num = num;
                    //see if the commercial break is before "wait" in seconds, this can be set to avoid going to commercial break too soon after a show starts
                    if (Double.Parse(num) > wait)
                    {
                        nums.Add(TimeSpan.FromSeconds(Double.Parse(num)));
                        Console.Write(".");
                    }
                    else
                    {
                        //black_frame was found before wait time
                        Console.Write("<");
                    }

                }
                else
                {
                    if (nums.Count > 0)
                    {
                        if (nums[nums.Count - 1].TotalSeconds != 0)
                        {
                            nums.Add(TimeSpan.FromSeconds(0));
                            Console.Write("+");
                        }
                    }
                }
            }
            Console.WriteLine("#");
            Console.WriteLine("Confirming Breaks...");
            for (int i = 1; i < nums.Count; i++)
            {
                Console.WriteLine(nums[i - 1]);
                if (nums[i - 1].TotalSeconds == 0 && nums[i].TotalSeconds == 0)
                {
                    nums.RemoveAt(i);
                    Console.Write("-");
                }
            }
            List<TimeSpan> commerical_breaks = new List<TimeSpan>();
            commerical_breaks.Add(TimeSpan.FromSeconds(0));
            int last = 0;
            for(int i = 0;i<nums.Count;i++) 
            {
                TimeSpan t = nums[i];
                //Console.WriteLine(i + " - " + t.ToString());
                if (t.TotalSeconds == 0) //start new count
                {
                    if (last != 0)
                    {

                        TimeSpan q = nums[last+1];
                        TimeSpan qa = nums[i-1];
                        //Console.WriteLine("Total: " + (qa - q).TotalSeconds + " - " + qa.ToString() + " - " + q.ToString());
                        AddLog("Total: " + (qa - q).TotalSeconds + " - " + qa.ToString() + " - " + q.ToString() + " ticks:" + (qa - q).Ticks + " thresh hold:" + TimeSpan.FromSeconds(threshhold).Ticks);
                        if ((qa - q).Ticks > TimeSpan.FromSeconds(threshhold).Ticks)
                        {

                            //Console.WriteLine("Found commercial break at " + qa.ToString());
                            AddLog("Found commercial break between (" + q.ToString() + "," + qa.ToString() + ")");
                            TimeSpan tdiff = qa - q;
                            AddLog("Using difference: " + (q + new TimeSpan(tdiff.Ticks / 2)).ToString());
                            commerical_breaks.Add((q + new TimeSpan(tdiff.Ticks / 2)));
                            Console.Write("*");
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
            Console.WriteLine("#");
            Console.WriteLine("Commercial breaks found: " + commerical_breaks.Count);
            AddLog("Commercial breaks found: " + commerical_breaks.Count);
            //Console.ReadKey();
            proc.Close();
            
            TimeSpan lt = new TimeSpan(0, 0, 0);
            List<TimeSpan> new_com = new List<TimeSpan>();
            foreach (TimeSpan t in commerical_breaks)
            {
                if (t.TotalSeconds - lt.TotalSeconds > min_time_add || lt.TotalMinutes == 0 || threshhold == 0)
                {
                    new_com.Add(t);
                    AddLog("New Found commercial break at " + t.ToString());
                    //Console.WriteLine("New Found commercial break at " + t.ToString());
                    Console.Write("!");
                }

                lt = t;
            }

            return new_com;
            
            //return commerical_breaks;
        }

        //Path.GetDirectoryName(ffmpeg_location) + "\\output\\log.txt"
        private static string Log_File = "";

        static void ResetLog()
        {
            if (Log_File == "") return;
            if (File.Exists(Log_File) == false) File.Create(Log_File + "\\log.txt").Close();
            File.WriteAllText(Log_File + "\\log.txt", string.Empty);
        }

        static void AddLog(string log)
        {
            if (Log_File == "") return;
            File.AppendAllText(Log_File + "\\log.txt", DateTime.Now.ToString() + "    " +  log + "\r\n");
        }

        static List<TimeSpan> breakz = new List<TimeSpan>();


        static void drawScreen(int which_one)
        {
            Console.Clear();
            //check and convert any non-mpg video to mpg
            switch (which_one)
            {
                case 1:
                    Console.WriteLine(@"  _   _                            _ _         ");
                    Console.WriteLine(@" | \ | |                          | (_)        ");
                    Console.WriteLine(@" |  \| | ___  _ __ _ __ ___   __ _| |_ _______ ");
                    Console.WriteLine(@" | . ` |/ _ \| '__| '_ ` _ \ / _` | | |_  / _ \");
                    Console.WriteLine(@" | |\  | (_) | |  | | | | | | (_| | | |/ /  __/");
                    Console.WriteLine(@" |_| \_|\___/|_|  |_| |_| |_|\__,_|_|_/___\___|");
                    break;
                case 2:
                    Console.Clear();
                    Console.WriteLine("  _____      _       _     ____                 _        ");
                    Console.WriteLine(" |  __ \\    (_)     | |   |  _ \\               | |       ");
                    Console.WriteLine(" | |__) | __ _ _ __ | |_  | |_) |_ __ ___  __ _| | _____ ");
                    Console.WriteLine(" |  ___/ '__| | '_ \\| __| |  _ <| '__/ _ \\/ _` | |/ / __|");
                    Console.WriteLine(" | |   | |  | | | | | |_  | |_) | | |  __/ (_| |   <\\__ \\");
                    Console.WriteLine(" |_|   |_|  |_|_| |_|\\__| |____/|_|  \\___|\\__,_|_|\\_\\___/");
                    break;
                case 3:
                    Console.WriteLine(@"  _________  _     ____ ______      __ __ ____ ___     ___  ___  ");
                    Console.WriteLine(@" / ___/    \| |   |    |      |    |  |  |    |   \   /  _]/   \ ");
                    Console.WriteLine(@"(   \_|  o  ) |    |  ||      |    |  |  ||  ||    \ /  [_|     |");
                    Console.WriteLine(@" \__  |   _/| |___ |  ||_|  |_|    |  |  ||  ||  D  |    _]  O  |");
                    Console.WriteLine(@" /  \ |  |  |     ||  |  |  |      |  :  ||  ||     |   [_|     |");
                    Console.WriteLine(@" \    |  |  |     ||  |  |  |       \   / |  ||     |     |     |");
                    Console.WriteLine(@"  \___|__|  |_____|____| |__|        \_/ |____|_____|_____|\___/ ");
                    break;
                case 4:
                    Console.WriteLine(@" ,----.                  ,-----.                       ,--.            ");
                    Console.WriteLine(@"'  .-./    ,---. ,--,--, |  |) /_,--.--. ,---.  ,--,--.|  |,-.  ,---.  ");
                    Console.WriteLine(@"|  | .---.| .-. :|      \|  .-.  \  .--'| .-. :' ,-.  ||     / (  .-'  ");
                    Console.WriteLine(@"'  '--'  |\   --.|  ||  ||  '--' /  |   \   --.\ '-'  ||  \  \ .-'  `) ");
                    Console.WriteLine(@" `------'  `----'`--''--'`------'`--'    `----' `--`--'`--'`--'`----'  ");
                    break;
                case 9:
                    Console.WriteLine(@"    )    (               (         )       )   (     ");
                    Console.WriteLine(@" ( /(    )\ )    *   )   )\ )   ( /(    ( /(   )\ )  ");
                    Console.WriteLine(@" )\())  (()/(  ` )  /(  (()/(   )\())   )\()) (()/(  ");
                    Console.WriteLine(@"((_)\    /(_))  ( )(_))  /(_)) ((_)\   ((_)\   /(_)) ");
                    Console.WriteLine(@"  ((_)  (_))   (_(_())  (_))     ((_)   _((_) (_))   ");
                    Console.WriteLine(@" / _ \  | _ \  |_   _|  |_ _|   / _ \  | \| | / __|  ");
                    Console.WriteLine(@"| (_) | |  _/    | |     | |   | (_) | | .` | \__ \  ");
                    Console.WriteLine(@" \___/  |_|      |_|    |___|   \___/  |_|\_| |___/  ");
                    break;
                default:
                    Console.WriteLine(@"____   ____.__    .___            _________      .__  .__  __   ");
                    Console.WriteLine(@"\   \ /   /|__| __| _/____  ____ /   _____/_____ |  | |__|/  |_ ");
                    Console.WriteLine(@" \   Y   / |  |/ __ |/ __ \/  _ \\_____  \\____ \|  | |  \   __\");
                    Console.WriteLine(@"  \     /  |  / /_/ \  ___(  <_> )        \  |_> >  |_|  ||  |  ");
                    Console.WriteLine(@"   \___/   |__\____ |\___  >____/_______  /   __/|____/__||__|  ");
                    Console.WriteLine(@"                   \/    \/             \/|__|                  ");
                    Console.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine("██████████████████████████████████████████████████████████");
                    Console.WriteLine("█                                                        █");
                    Console.WriteLine("█ Options:                                               █");
                    Console.WriteLine("█                                                        █");
                    Console.WriteLine("█   [ 1 ] - Batch Normalize Audio                        █");
                    Console.WriteLine("█   [ 2 ] - Print Breaks                                 █");
                    Console.WriteLine("█   [ 3 ] - Split Video                                  █");
                    Console.WriteLine("█   [ 4 ] - Generate Breaks                              █");
                    Console.WriteLine("█                                                        █");
                    Console.WriteLine("█   [ 9 ] - Options                                      █");
                    Console.WriteLine("█                                                        █");
                    Console.WriteLine("██████████████████████████████████████████████████████████");
                    break;
            }
            Console.WriteLine();
            Console.WriteLine();
        }

        static void drawMessage(string message)
        {
            Console.Clear();
            Console.WriteLine(@" _______  ___      _______  ______    _______ ");
            Console.WriteLine(@"|   _   ||   |    |       ||    _ |  |       |");
            Console.WriteLine(@"|  |_|  ||   |    |    ___||   | ||  |_     _|");
            Console.WriteLine(@"|       ||   |    |   |___ |   |_||_   |   |  ");
            Console.WriteLine(@"|       ||   |___ |    ___||    __  |  |   |  ");
            Console.WriteLine(@"|   _   ||       ||   |___ |   |  | |  |   |  ");
            Console.WriteLine(@"|__| |__||_______||_______||___|  |_|  |___|  ");
            Console.WriteLine("");
            Console.WriteLine("*****************************************************************");
            string[] lines = message.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (string msg in lines)
            {
                Console.WriteLine(">>>>>     " + msg);
            }
            Console.WriteLine("*****************************************************************");
            Console.WriteLine("");
            Console.WriteLine("Press a key to continue!");
            Console.ReadKey();
        }

        static void Main(string[] args)
        {

            string app_path = AppDomain.CurrentDomain.BaseDirectory.ToString();
            if(File.Exists(app_path + "\\settings.txt"))
            {
                string[] lines = File.ReadAllLines(app_path + "\\settings.txt");
                for (int i = 0; i < lines.Count(); i++)
                {
                    string[] opt = lines[i].Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                    switch(opt[0].ToLower().Trim())
                    {
                        case "ffmpeg location":
                            Console.WriteLine("Setting FFMPEG location to " + opt[1]);
                            ffmpeg_location = opt[1];
                            break;
                        case "alt ffmpeg location":
                            Console.WriteLine("Setting Alt FFMPEG location to " + opt[1]);
                            ffmpegX_location = opt[1];
                            break;
                        case "temp folder":
                            Console.WriteLine("Setting Temp Folder to " + opt[1]);
                            temp_folder = opt[1];
                            break;
                        case "log file":
                            Console.WriteLine("Setting Log File location to " + opt[1]);
                            Log_File = opt[1];
                            break;
                        default:

                            break;
                    }
                }
            }
            ResetLog();

            System.Threading.Thread.Sleep(2000);

            /*
            string folder = Path.GetDirectoryName(ffmpeg_location);

            if (Directory.Exists(folder + "\\shows\\") == false) Directory.CreateDirectory(folder + "\\shows\\");
            List<string> shows = GetFiles(folder + "\\shows\\");
            //List<string> shows = GetFiles(@\shows\");
            if (Directory.Exists(folder + "\\output\\") == false) Directory.CreateDirectory(folder + "\\output\\");
            List<string> finished_shows = GetFiles(folder + "\\output\\");
            if (Directory.Exists(folder + "\\convert\\") == false) Directory.CreateDirectory(folder + "\\convert\\");
            List<string> commercials = GetFiles(folder + "\\convert\\");
            if (Directory.Exists(folder + "\\bopen\\") == false) Directory.CreateDirectory(folder + "\\bopen\\");
            List<string> bumpers_open = GetFiles(folder + "\\bopen\\");
            if (Directory.Exists(folder + "\\bclose\\") == false) Directory.CreateDirectory(folder + "\\bclose\\");
            List<string> bumpers_close = GetFiles(folder + "\\bclose\\");
            */

            while (1 == 1)
            {
                drawScreen(0); //draw the selection screen

                ConsoleKeyInfo ck = Console.ReadKey();
                char selected_option = ck.KeyChar;

                if (ck.Key == ConsoleKey.Escape) Environment.Exit(0);
                if (ffmpeg_location == "") selected_option = '9';
                switch (selected_option)
                {
                    case '1':
                        //normalize
                        drawScreen(1); //normalize

                        List<string> paths = new List<string> { };
                        bool add_paths = true;
                        string normal_audio_folder = "";
                        while (add_paths)
                        {
                            add_paths = false;
                            Console.WriteLine("Enter path to video files: (leave blank to skip)");
                            normal_audio_folder = Console.ReadLine();
                            if (normal_audio_folder == "") break;

                            if (normal_audio_folder.Substring(0, 1) == "+")
                            {
                                normal_audio_folder = normal_audio_folder.Substring(1);
                                add_paths = true;
                            }

                            if (!Directory.Exists(normal_audio_folder))
                            {
                                drawMessage("Path does not exist");
                            }
                            else
                            {
                                paths.Add(normal_audio_folder);
                                Console.WriteLine();
                                Console.WriteLine(normal_audio_folder + " added to queue (" + paths.Count.ToString() + ")");
                                Console.WriteLine();
                                Console.WriteLine();
                            }
                        }

                        TimeSpan tempTotal = TimeSpan.FromSeconds(0);

                        for (int i = 0; i < paths.Count; i++)
                        {

                            normal_audio_folder = paths[i];

                            if (normal_audio_folder != "")
                            {
                                List<string> normal_audio = GetFiles(normal_audio_folder, video_file_extensions, new string[] { "_NA_" });

                                if (normal_audio.Count > 0)
                                {
                                    int nac = 0;
                                    int nacm = normal_audio.Count;

                                    TimeSpan filter = TimeSpan.FromSeconds(0);
                                    TimeSpan apply = TimeSpan.FromSeconds(0);
                                    TimeSpan tl = TimeSpan.FromSeconds(0);

                                    while (normal_audio.Count > 0)
                                    {

                                        drawScreen(1);

                                        tempTotal = filter.Add(apply);

                                        Console.WriteLine("Estimated time remaining: " + tl.Hours.ToString().PadLeft(2, '0') + "h" + tl.Minutes.ToString().PadLeft(2, '0') + "m" + tl.Seconds.ToString().PadLeft(2, '0') + "s Elapsed: " + tempTotal.Hours.ToString().PadLeft(2, '0') + "h" + tempTotal.Minutes.ToString().PadLeft(2, '0') + "m" + tempTotal.Seconds.ToString().PadLeft(2, '0') + "s");
                                        Console.WriteLine("");

                                        Console.WriteLine("Normalizing audio for: " + Path.GetFileName(normal_audio[0]));
                                        Console.WriteLine("File " + (nac + 1).ToString() + " of " + nacm.ToString());
                                        Console.WriteLine();
                                        TimeSpan[] vals = NormalizeAudio(normal_audio[0]);

                                        filter = filter.Add(vals[0]);
                                        apply = apply.Add(vals[1]);

                                        double timeleft = ((filter.TotalSeconds + apply.TotalSeconds) / (nac + 1)) * (nacm - (nac + 1));

                                        tl = TimeSpan.FromSeconds(timeleft);



                                        if (filter.TotalSeconds == 0)
                                        {
                                            nacm--;
                                        }
                                        else
                                        {
                                            nac++;
                                        }
                                        normal_audio.RemoveAt(0);
                                    }
                                }
                            }

                        }

                        drawMessage("Normalization complete!" + Environment.NewLine + "Total time: " + tempTotal.Hours.ToString().PadLeft(2, '0') + "h" + tempTotal.Minutes.ToString().PadLeft(2, '0') + "m" + tempTotal.Seconds.ToString().PadLeft(2, '0') + "s");

                        break;
                    case '2': //print breaks
                        List<string> print_breaks = new List<string>(); // GetFiles(@"G:\Projects\Video\torrents\Murder She Wrote - Season 4");// folder + "\\print_breaks\\");

                        drawScreen(2);//print breaks

                        Console.WriteLine("Enter path to video files: (leave blank to skip)");
                        string print_folder = Console.ReadLine();
                        if (print_folder == "") break;

                        if (!Directory.Exists(print_folder))
                        {
                            drawMessage("Path does not exist");
                            break;
                        }

                        string print_breaks_folder = print_folder; // @"H:\tv station\specials\football";
                        print_breaks = GetFiles(print_breaks_folder, video_file_extensions);

                        if (print_breaks.Count > 0)
                        {


                            Console.WriteLine("");
                            Console.WriteLine("");
                            Console.WriteLine("Enter threshold: (length of time to be considered, default 0.5 seconds)");
                            string thresh = Console.ReadLine();
                            double ithresh = 0.5;
                            try
                            {
                                if (thresh != "") ithresh = Double.Parse(thresh);
                            }
                            catch { }

                            Console.WriteLine("Threshold set to " + ithresh.ToString() + " sec(s)");

                            Console.WriteLine("");
                            Console.WriteLine("");
                            Console.WriteLine("Enter minimum start time: (length of time to be considered, default 0 seconds)");
                            string dwait = Console.ReadLine();
                            double iwait = -1;
                            try
                            {
                                if (dwait != "") iwait = Double.Parse(dwait);
                            }
                            catch { }

                            Console.WriteLine("Minimum start time is " + iwait.ToString() + " sec(s)");

                            while (print_breaks.Count > 0)
                            {
                                Console.WriteLine("Printing breaks for:" + print_breaks[0]);

                                string spb_output = print_breaks_folder + "\\" + Path.GetFileName(print_breaks[0]) + ".commercials";

                                if (!File.Exists(spb_output))
                                {
                                    List<TimeSpan> cms = scanForCommercialBreaks(print_breaks[0], 0.5, iwait);
                                    Console.Write(cms.Count.ToString());
                                    //Console.ReadKey();
                                    string spb = "";
                                    cms.RemoveAt(0);
                                    foreach (TimeSpan t in cms)
                                    {
                                        AddLog("Found commercial at (in seconds): " + Math.Floor(t.TotalSeconds).ToString());
                                        //AddLog("Found commercial at (in seconds): " + (t.TotalSeconds).ToString());
                                        spb += Math.Floor(t.TotalSeconds).ToString() + "\n";
                                        //spb += (t.TotalSeconds).ToString() + "\n";

                                    }
                                    if (spb != "")
                                    {
                                        File.WriteAllText(spb_output, spb.Substring(0, spb.Length - 1));
                                        AddLog("Prnted breaks: " + spb_output);
                                    }
                                    else { AddLog("No breaks found"); }

                                }
                                else
                                {
                                    AddLog("Commercials Already exist for this show.");
                                    Console.WriteLine("Commercials Already exist for this show.");
                                }
                                print_breaks.RemoveAt(0);
                            }

                            //return;
                        }
                        drawMessage("Print Breaks complete!");
                        break;
                    case '3': //split video
                        drawScreen(3); //split
                        Console.WriteLine("Enter path to video files:");
                        string split_in_folder = Console.ReadLine();

                        if (split_in_folder == "") break;

                        if (!Directory.Exists(split_in_folder))
                        {
                            drawMessage("Path does not exist");
                            break;
                        }

                        Console.WriteLine("Enter output path : (leave blank to create new folder \\output\\)");
                        string split_out_folder = Console.ReadLine();

                        if (split_out_folder == "")
                        {
                            split_out_folder = split_in_folder + "\\output\\";
                            if (!Directory.Exists(split_out_folder))
                            {
                                Directory.CreateDirectory(split_out_folder);
                            }
                            else
                            {
                                drawMessage("Output folder already exists" + Environment.NewLine + "Any files currently in there may be overwritten");
                            }
                        }
                        if (!Directory.Exists(split_out_folder))
                        {
                            drawMessage("Path does not exist");
                            break;
                        }

                        checkSplits(split_in_folder,split_out_folder);

                        drawMessage("Splits have been checked!");
                        AddLog("Splits have been checked.");
                        //Console.ReadKey();
                        break;
                    case '4':
                        //print generic breaks
                        drawScreen(4);
                        //getDurationAndAudioFilter
                        Console.ReadKey();
                        break;

                    case '9':
                        //options
                        drawScreen(9); //options

                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine("Enter FFMPEG Location: ");
                        if (ffmpeg_location != "") SendKeys.SendWait(ffmpeg_location);
                        string flocation = Console.ReadLine();

                        if (File.Exists(flocation) == false)
                        {
                            drawMessage("Could not verify file existance!");
                            break;
                        }

                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine("Enter Alt FFMPEG Location: ");
                        if (ffmpeg_location != "") SendKeys.SendWait(ffmpegX_location);
                        string xlocation = Console.ReadLine();

                        if (File.Exists(xlocation) == false)
                        {
                            drawMessage("Could not verify file existance!");
                            break;
                        }

                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine("Enter TEMP Path: (if it doesn't exist, it'll be created)");
                        if (temp_folder != "") SendKeys.SendWait(temp_folder);
                        string tlocation = Console.ReadLine();

                        if (Directory.Exists(tlocation) == false)
                        {
                            Directory.CreateDirectory(tlocation);
                        }

                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine("Enter Log File Location: (if it doesn't exist, it'll be created)");
                        if (temp_folder != "") SendKeys.SendWait(temp_folder);
                        string llocation = Console.ReadLine();

                        if (Directory.Exists(llocation) == false)
                        {
                            Directory.CreateDirectory(llocation);
                        }

                        ffmpeg_location = flocation;
                        ffmpegX_location = xlocation;
                        temp_folder = tlocation;
                        Log_File = llocation;

                        File.WriteAllText("settings.txt", "ffmpeg location=" + ffmpeg_location + Environment.NewLine + "alt ffmpeg location=" + ffmpegX_location + Environment.NewLine + "temp folder=" + temp_folder + Environment.NewLine + "log file=" + temp_folder + Environment.NewLine);

                        break;
                    default:
                        Console.CursorVisible = false;
                        drawMessage("Invalid Entry!");
                        break;
                }
            }

            

            /*

            if (File.Exists(folder + "\\rnd.txt")) //create 15 random commercial videos
            {
                Console.WriteLine("Nice! Gonna make some randon commercial blocks...");
                Console.ReadKey();
                for (int i = 0; i < 15; i++)
                {
                    string tmp_comm = new Foo().getSomeCommercials(Path.GetDirectoryName(ffmpeg_location), 5);
                    Console.WriteLine("Got the commercials..." + tmp_comm);

                    joinConcat(tmp_comm);

                    Console.WriteLine("Random Commercials saved!");
                }
                File.Delete(folder + "\\rnd.txt"); //delete it so we don't keep creating rnd videos.
                Console.ReadKey();
            }


            Console.WriteLine("Checking " + commercials.Count + " conversion file(s)....");
            AddLog("Checking to see if any files need to be converted to MP4");
            checkCommercials();
            Console.WriteLine("Files have been checked! Press any Key to continue....");
            AddLog("Conversions have been checked.");
            Console.ReadKey();
            Console.WriteLine("Checking for video to split....");
            AddLog("Checking to see if any videos need to be split.");


            Console.WriteLine("Checking for video to join....");
            AddLog("Checking to see if any videos need to be joined.");
            joinVideos();
            Console.WriteLine("Joins have been checked! Press any Key to continue....");
            AddLog("Joins have been checked.");
            Console.ReadKey();
            
            while(shows.Count > 0)
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
                    breakz = scanForCommercialBreaks(shows[0].ToString(), thresh, 0);
                }

                Console.WriteLine("Found all the breaks, press any key...");
                //Console.ReadKey();

                AddLog("Commercial breaks scan complete. Found " + breakz.Count.ToString() + " commercial breaks.");
                Console.WriteLine(breakz.Count.ToString());
                //Console.ReadKey();
                AddLog("Actual show conversion is beginning...");
                convertShow(shows[0].ToString(), breakz);
                AddLog("Show has been converted.");
                emptyTemp();
                Console.WriteLine("All done!");
                shows.RemoveAt(0);

            }

            //convertShow(folder + "comm\\80sKidsCommercials-1987-volume16685.mpg");

            //-vf "blackdetect=d=2:pix_th=0.00"
            emptyTemp();
            Console.WriteLine("All done! Press a key.");
            Console.ReadKey();
            AddLog("END LOG");
            Environment.Exit(0);
            return;
           
            */
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

                    proc.StartInfo.Arguments = "-i " + filename + " -bsf:v h264_mp4toannexb -f mpegts " + audio_filter + " -vf scale=640:480 -r 29.97 -y -b:v 2M -ss-ss " + start_str + " -t " + length_str + " c:\\ffmpeg\\temp\\temp" + (z-1).ToString() + ".mpg";
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

                //str += f + "|";
                str += "file '" + f.Replace("\\", "\\\\") + "'\n";
                
                
            }
            Console.Write(str);
            //Console.ReadKey();

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
