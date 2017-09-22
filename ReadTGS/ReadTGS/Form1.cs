using System; //test
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;       //Microsoft Excel 14 object in references-> COM tab
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;


namespace ReadTGS
{
    public partial class Form1 : Form
    {
        class tgsitemclass 
        {
            public double hours = 0;
            public int category = 0; //Undervisning = 0, FoU/komputv = 1, övrigt = 2
            public string label = "";
            public List<string> coursecodes = new List<string>();
            public int objekt = 0;
            public string print()
            {
                string s = category+"\t"+label + "\t" + objekt  + "\t" + hours;
                foreach (string cs in coursecodes)
                    s += "\t" + cs;
                
                return s;
            }
            public bool read_print(string[] words)
            {
                if ( words.Length < 4)
                    return false;

                category = tryconvert(words[0]);
                if (category < 0)
                    return false;

                label = words[1].Trim();
                objekt = tryconvert(words[2]);
                hours = tryconvert0(words[3]);
                if (words.Length > 4)
                    for (int i = 4; i < words.Length;i++ )
                    {
                        coursecodes.Add(words[i]);
                    }

                return true;
            }
        }

        class teacherclass
        {
            public string name = "";
            public string teacherID = ""; //signature
            public string birthday = "";
            public string firstname = "";
            public string lastname = "";
            public string subject = "";
        }

        class coursepengclass
        {
            public string utb_omr;
            public float hst;
            public float hpr;
        }

        class courseclass
        {
            public int courseID;
            public string coursecode = ""; //ladok-kod, PG1044 etc.
            public string applicationcode = ""; //anmälningskod, V2JBF etc
            public string applied_as = ""; //paketets kod för kurser i kurspaket
            public string name = "";
            public bool ht = true; //false if vt
            public int year = 2016;
            public string language = ""; //undervisningsspråk
            public int ffgreg = 0; //antal förstagångregistrerade
            public int paying = 0;  //betalande stud
            public int exchange = 0; //utbytesstud inom avtal
            public int dropout = 0;
            public int earlydropout = 0;
            public float age = 0; //average age
            public float men = 0; //% men
            public float hp = 0;
            public int studyrate = 0; //studietakt %
            public bool distance = false;
            public string city = "";
            public string dayevening = "";
            public int startv = 0;
            public int slutv = 0;
            public string hptermin = "";
            public int fee = 0;
            public float hst = 0;
            public float hpr = 0;
            public int budget_stud = 0;
            public int budget_bet_stud = 0;
            public float budget_klt = 0;
            public float budget_klt_egen = 0;
            public float budget_klt_tillf = 0;
            public float budget_klt_konsult = 0;
            public float budget_prestgrad = 0;
            public float budget_kursfaktor = 0;
            public string utb_omr = "";
            public List<coursepengclass> cplist = new List<coursepengclass>();

            public string print()
            {
                string s = "";
                s += courseID.ToString() + "\t" + coursecode + "\t" + applicationcode + "\t" +
                    name + "\t" + hp.ToString() + "\t" + ht.ToString() + "\t" + year.ToString() + "\t" + ffgreg.ToString() + "\t" +
                    distance.ToString() + "\t" + city + "\t" + dayevening + "\t" + startv + "\t" + slutv;
                return s;
            }
        }

        class orgsubjectclass
        {
            public string osname = "";
            public string academy = "";
            public string department = "";
            public int objekt = -1;
        }

        class academyclass
        {
            public string acid = "???";
            public string label = "Unknown";

        }
        Dictionary<int, academyclass> acdict = new Dictionary<int, academyclass>();

        static List<tgssheetclass> tgslist = new List<tgssheetclass>();
        static List<teacherclass> teacherlist = new List<teacherclass>();
        static List<courseclass> courselist = new List<courseclass>();
        static int maxfiles = 99999;
        static Dictionary<string, string> extra_sigdict = new Dictionary<string, string>();
        static Dictionary<string, string> subjectfolderdict = new Dictionary<string, string>();
        static Dictionary<string, string> subjectnamedict = new Dictionary<string, string>();
        static Dictionary<string, string> subjectcodedict = new Dictionary<string, string>();
        static Dictionary<string, string> courseorgdict = new Dictionary<string, string>();
        static List<orgsubjectclass> orgsubjectlist = new List<orgsubjectclass>();
        static Dictionary<string, string> altsubjectdict = new Dictionary<string, string>(); //alternative names for subject
        static Dictionary<string, string> progcodedict = new Dictionary<string, string>();
        static Dictionary<string, string> progsubjdict = new Dictionary<string, string>();
        static Dictionary<string, string> progleveldict = new Dictionary<string, string>();
        static Dictionary<string, string> batchentrydict = new Dictionary<string, string>();

        static string academicyear = "16-17";
        static int tgsyear = 2016;
        static bool ht = true;
        static string academy = "HM";

        static string connectionstring = "Data Source=db-tgsanalys-test.du.se;Initial Catalog=dbTGSAnalysTest;Integrated Security=True;Pooling=False";
        static DbTGSAnalysTest db = null;
        bool really_submit = true;

        class tgssheetclass
        {
            public bool is_ht = false;
            public string teachername = "";
            public string birthday = "";
            public string teacherID = ""; //signature
            public string subject = "";
            public List<tgsitemclass> tgsitems = new List<tgsitemclass>();
            public double totaltodo = 0;
            public double tjlsjuk1 = 0;
            public double tjlsjuk2 = 0;
            public double in_sparadsem = 0;
            public double ut_sparadsem = 0;
            public double remainstodo = 0;
            public double[] sums = new double[] { 0, 0, 0 };
            public double totaldone = 0;
            public double overunder = 0;
            public double adjustment = 0;
            public DateTime modified;
            public bool definitive = false;
            public string academicyear = "";
            public string filename = "";
            public bool best = false; //true if this is the best tgs version for this teacher this semester

            public static string tableheader()
            {
                string s =  "\t\t\t\tTotalt\tTjänstl.\tTjänstl.\tIngående\tUtgående\tAtt\tSumma\tÖver/\t\t\n";
                s += "Namn\tSig\tFödd\tÄmne\tatt göra\tsjuk 1\tsjuk 2\tsparad sem\tsparad sem\tgöra\tgjort\tunder\tJustering\tDatum\tLäsår\tTermin\tDefinitiv";
                return s;
            }

            public bool read_dataline(string[] words)
            {
                if (words.Length < 16)
                    return false;

                teachername = words[0];
                teacherID = words[1];
                birthday = words[2];
                subject = words[3];
                totaltodo = tryconvert(words[4]);
                tjlsjuk1 = tryconvert(words[5]);
                tjlsjuk2 = tryconvert(words[6]);
                in_sparadsem = tryconvert(words[7]);
                ut_sparadsem = tryconvert(words[8]);
                remainstodo = tryconvert(words[9]);
                totaldone = tryconvert(words[10]);
                overunder = tryconvert(words[11]);
                adjustment = tryconvert(words[12]);
                modified = DateTime.Parse(words[13]);
                academicyear = words[14];
                is_ht = (words[15] == "ht");
                definitive = (words[16] == "True");
                best = true;

                return true;
            }

            public string dataline()
            {
                string s = "";
                string termin = "vt";
                if (is_ht)
                    termin = "ht";
                s += teachername + "\t" + teacherID + "\t" + birthday + "\t" + subject + 
 "\t" + totaltodo + "\t" + tjlsjuk1 + "\t" + tjlsjuk2 + "\t" + in_sparadsem + "\t" + ut_sparadsem;
                s += "\t" + remainstodo + "\t" + totaldone + "\t" + overunder + "\t" 
   + adjustment + "\t" + modified.ToShortDateString() + "\t" + academicyear + "\t" + termin + "\t" + definitive.ToString();
                return s;
            }

            public int year()
            {
                if (is_ht)
                    return (tryconvert(academicyear.Substring(0, 2))+2000);
                else
                    return (tryconvert(academicyear.Substring(3, 2)) + 2000);
            }

            public string getacademy(string filename)
            {
                if (filename.Contains(@"\HM\"))
                    return "HM";
                if (filename.Contains(@"\UHS\"))
                    return "UHS";
                if (filename.Contains(@"\IoS\"))
                    return "IoS";
                
                return "###";
            }

            public string checksum()
            {
                string s = "";
                double sum = 0;
                foreach (tgsitemclass tt in tgsitems)
                    sum += tt.hours;
                s += teachername + "\t" + teacherID + "\t" + subject + "\t" + 
                    (int)sum + "\t" + (int)totaldone + "\t" + (int)(totaldone - remainstodo) + "\t" + (int)overunder + "\t";
                if (sum != totaldone)
                    s += "Wrong sum!";
                s += "\t";
                if ( totaldone-remainstodo != overunder)
                    s += "Wrong overunder!";
                return s;
            }

            public void ReadSheet(Excel.Worksheet xll, bool ht, string fname)
            {
                Excel.Range range;
                range = xll.UsedRange;
                int rw = range.Rows.Count;
                int cl = range.Columns.Count;

                if (cl < 19)
                {
                    teachername = "### Not enough columns";
                    return;                
                }
                
                if (rw < 30)
                {
                    teachername = "### Not enough rows";
                    return;
                }
                if (!getstring(xll.Cells[1, 1]).Contains("Högskolan Dalarna"))
                {
                    if (!getstring(xll.Cells[1, 17]).Contains("äsår"))
                    {

                        teachername = "### Högskolan Dalarna & läsår missing";
                        return;
                    }
                }

                filename = fname;

                //set hourcol for small base table:
                int hourcol = 18; //vt
                if (ht)
                    hourcol = 17;

                int namecol = 1;
                teachername = "";
                do
                {
                    namecol++;
                    teachername = getstring(xll.Cells[3, namecol]);
                    if ( (namecol > 10 ) || (teachername.Contains("ödelsedata") ))
                    {
                        teachername = "";
                        break;
                    }
                }
                while (String.IsNullOrEmpty(teachername));
                if (String.IsNullOrEmpty(teachername) )
                {
                    teachername = filename + ":" + xll.Name;
                    
                }

                //if ( !String.IsNullOrEmpty(getstring(xll,1,7).Trim()))
                //{
                //    teachername = "### prel " + teachername;
                //    return;
                //}

                birthday = getstring(xll.Cells[3, 14]);
                if (birthday.Length > 6)
                {
                    if (( birthday.Substring(0,2) == "19") && (birthday.Length >= 8))
                        birthday = birthday.Substring(2, 6);
                    else
                        birthday = birthday.Substring(0, 6);
                }

                int subjectcol = 9;
                subject = "";
                do
                {
                    subjectcol++;
                    subject = getstring(xll.Cells[6, subjectcol]);
                    if ((namecol > 15) || (subject.Contains("mfattning")))
                    {
                        subject = "";
                        break;
                    }
                }
                while (String.IsNullOrEmpty(subject));

                if (String.IsNullOrEmpty(subject))
                    subject = filename;
                else
                    subject = subject.Trim().ToLower();
                
                definitive = !String.IsNullOrEmpty(getstring(xll, 1, 10).Trim());
                academicyear = getstring(xll, 1, 18);
                is_ht = ht;

                int baserow = 13;
                int tgsrow = 23;

                totaltodo = getdouble(xll.Cells[baserow, hourcol].Value);
                tjlsjuk1 = getdouble(xll.Cells[baserow+1, hourcol].Value);
                tjlsjuk2 = getdouble(xll.Cells[baserow+2, hourcol].Value);
                in_sparadsem = getdouble(xll.Cells[baserow+3, hourcol].Value);
                ut_sparadsem = getdouble(xll.Cells[baserow+4, hourcol].Value);
                remainstodo = getdouble(xll.Cells[baserow+8, hourcol].Value);
                int section = 0;

                int lastitem = -1;

                //change hourcol for main TGS table
                if (ht) 
                    hourcol = 14;
                else
                    hourcol = 17;

                //loop over actual tgs lines
                for (int i = tgsrow; i <= rw ; i++)
                {
                    string firstcol = getstring(xll.Cells[i, 1]);
                    if (String.IsNullOrEmpty(firstcol))
                    {
                        for (int icol = 1; icol < 12; icol++)
                            firstcol += getstring(xll, i, icol);
                    }
                    firstcol = firstcol.Trim().ToLower();
                    if (String.IsNullOrEmpty(firstcol))
                        continue;

                    double hh = getdouble(xll.Cells[i,hourcol].Value);
                    if ( hh > 0)
                    {
                        if (firstcol.Contains("justering över - undertid"))
                        {
                            adjustment = hh;
                        }
                        else
                        {
                            tgsitemclass tt = new tgsitemclass();
                            tt.hours = hh;
                            if (firstcol.Contains("ompetensutveckling 5%"))
                            {
                                tt.category = 3;
                            }
                            else if (firstcol.Contains("gen adm, möten, kollektiv tid 5%"))
                            {
                                tt.category = 4;
                            }
                            else
                                tt.category = section;
                            tt.label = firstcol;
                            foreach (string cc in getcoursecode(firstcol))
                            {
                                tt.label = tt.label.Replace(cc, "").Trim();
                                tt.coursecodes.Add(standardcoursecode(cc));
                            }
                            tt.objekt = Convert.ToInt32(getdouble(xll.Cells[i, 12].Value));
                            tgsitems.Add(tt);
                            lastitem = i;
                        }
                    }
                    else 
                    {
                        if (firstcol.Contains("fou,") || firstcol.Contains("ompetensutveckling"))
                            section = 1;
                        else if (firstcol.Contains("övrigt arbete"))
                            section = 2;
                    }
                }

                //get the bottom information
                for (int i = lastitem; i <= rw; i++)
                {
                    string label = getstring(xll,i,12);
                    if (label.Contains("total"))
                        totaldone = getdouble(xll.Cells[i-1, hourcol].Value);
                    else if (label.Contains("underskott/överskott efter h"))
                        overunder = getdouble(xll.Cells[i-1, hourcol].Value);

                }
            }



            public static double getdouble(dynamic cell)
            {
                double i = 0;
                if (cell == null)
                    return 0;

                try
                {
                    i = cell;
                }
                catch (Exception e)
                {
                    return 0;
                }

                return i;
            }

            public static string getstring(Excel.Worksheet xll,int row,int col)
            {
                try
                {
                    return getstring(xll.Cells[row, col]);
                }
                catch (Exception e)
                {
                    return "";
                }

            }

            public static string getstring(dynamic cell)
            {
                
                if (cell == null)
                    return "";

                try
                {
                    if (cell == null)
                        return "";
                    if (cell.Value == null)
                        return "";
                    return cell.Value.Trim();
                }
                catch (Exception e)
                {
                    return "";
                }

                
            }
            

            public static List<string> getcoursecode(string s)
            {
                List<string> rl = new List<string>();

                string regexcode = @"\p{L}{2} ?\d{1}[xX\?\d]{3}";

                Match m = Regex.Match(s, regexcode, RegexOptions.IgnoreCase);
                while (m.Success)
                {
                    rl.Add(m.Groups[0].Value);
                    m = m.NextMatch();
                }

                return rl;
            }

            public static List<string> getapplcode(string s)
                //finds strings of form "(Vxxxx)" and "(Hxxxx)"
            {
                List<string> rl = new List<string>();

                string regexcode = @"\(\S{5}\)";

                Match m = Regex.Match(s, regexcode, RegexOptions.IgnoreCase);
                while (m.Success)
                {
                    rl.Add(m.Groups[0].Value.Replace("(","").Replace(")","").ToUpper());
                    m = m.NextMatch();
                }

                return rl;
            }

            public static List<float> gethp(string s)
            //finds strings of form "7.5 hp" and "7,5hp"
            {
                List<float> rl = new List<float>();

                string regexcode = @"[.,\d]+ ?hp";

                Match m = Regex.Match(s.ToLower(), regexcode, RegexOptions.IgnoreCase);
                while (m.Success)
                {
                    rl.Add((float)tryconvertdouble(m.Groups[0].Value.Replace("hp","")));
                    m = m.NextMatch();
                }

                return rl;
            }

            public static string standardcoursecode(string s)
            {
                return s.Replace(" ", "").ToUpper();
            }
        

        }

        public static hbookclass course_codematchhist = new hbookclass();
        public static hbookclass course_bestmatchhist = new hbookclass();
        public static hbookclass languagehist = new hbookclass();


        public static string getdatestring()
        {
            DateTime thismonth = DateTime.Now;
            string monthstring = thismonth.Month.ToString();
            while (monthstring.Length < 2)
                monthstring = "0" + monthstring;
            string daystring = thismonth.Day.ToString();
            while (daystring.Length < 2)
                daystring = "0" + daystring;
            return thismonth.Year.ToString() + monthstring + daystring;
        }


        public void memo(string line)
        {
            richTextBox1.AppendText(line + "\n");
            richTextBox1.ScrollToCaret();
        }


        public Form1()
        {
            InitializeComponent();
            filldicts();
            getacademicyear();
        }

        private void QuitButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        public static int tryconvert(string word)
        {
            int i = -1;

            if (word.Length == 0)
                return i;

            try
            {
                i = Convert.ToInt32(word);
            }
            catch (OverflowException)
            {
                Console.WriteLine("i Outside the range of the Int32 type: " + word);
            }
            catch (FormatException)
            {
                //if ( !String.IsNullOrEmpty(word))
                //    Console.WriteLine("i Not in a recognizable format: " + word);
                if (word.Contains(" "))
                    i = tryconvert(word.Replace(" ", ""));
            }

            return i;

        }

        public static int tryconvert0(string word)
        {
            int i = 0;

            if (word.Length == 0)
                return i;

            try
            {
                i = Convert.ToInt32(word);
            }
            catch (OverflowException)
            {
                Console.WriteLine("i Outside the range of the Int32 type: " + word);
            }
            catch (FormatException)
            {
                //if ( !String.IsNullOrEmpty(word))
                //    Console.WriteLine("i Not in a recognizable format: " + word);
                if (word.Contains(" "))
                    i = tryconvert(word.Replace(" ", ""));
            }

            return i;

        }

        public static long tryconvertlong(string word)
        {
            long i = -1;

            if (word.Length == 0)
                return i;

            try
            {
                i = Convert.ToInt64(word);
            }
            catch (OverflowException)
            {
                Console.WriteLine("i Outside the range of the Int32 type: " + word);
            }
            catch (FormatException)
            {
                //if ( !String.IsNullOrEmpty(word))
                //    Console.WriteLine("i Not in a recognizable format: " + word);
            }

            return i;

        }

        public static double tryconvertdouble(string word)
        {
            double i = -1;

            if (word.Length == 0)
                return i;

            try
            {
                i = Convert.ToDouble(word);
            }
            catch (OverflowException)
            {
                Console.WriteLine("i Outside the range of the Double type: " + word);
            }
            catch (FormatException)
            {
                try
                {
                    i = Convert.ToDouble(word.Replace(".", ","));
                }
                catch (FormatException)
                {
                    //Console.WriteLine("i Not in a recognizable double format: " + word.Replace(".", ","));
                }
                //Console.WriteLine("i Not in a recognizable double format: " + word);
            }

            return i;

        }

        private int courselevel(string coursecode)
        {
            int cl = 1;
            if (coursecode.Length > 2)
            {
                string cc = coursecode.Substring(2, 1);
                cl = tryconvert(cc);
                if (cl < 0)
                {
                    if (cc == "D")
                        cl = 3;
                    else if (cc == "C")
                        cl = 2;
                    else
                        cl = 1;
                }
            }
            return cl;
        }



        private List<string> get_filelist(string dir)
        {
            List<string> fl = new List<string>();

            string[] fs = Directory.GetFiles(dir);
            foreach (string f in fs)
                fl.Add(f);

            string[] ds = Directory.GetDirectories(dir);
            foreach (string subdir in ds)
                foreach (string f in get_filelist(subdir))
                    fl.Add(f);

            return fl;
        }

        private List<tgssheetclass> ReadTGSFile(string filename)
        {
            List<tgssheetclass> returnlist = new List<tgssheetclass>();
            
            memo(filename);
            if (filename.IndexOf("~") >= 0)
            {
                //tgs.teachername = "Bad filename";
                return returnlist;
            }
            if (!filename.Contains(".xls"))
            {
                //tgs.teachername = "Not excel file";
                return returnlist;
            }
            Excel.Application xlApp = new Excel.Application();
            Excel.Workbook xlWorkBook;
            //Excel.Worksheet xlvtl;
            //Excel.Worksheet xlhtl;

            

            xlWorkBook = xlApp.Workbooks.Open(filename, 0, true, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
            //xlvtl = xlWorkBook.Sheets[1];
            //xlhtl = xlWorkBook.Sheets[6];

            List<Excel.Worksheet> sheetlist = new List<Excel.Worksheet>();

            foreach (Excel.Worksheet xll in xlWorkBook.Sheets)
            {
                sheetlist.Add(xll);
                
                tgssheetclass tgs = new tgssheetclass();
                tgs.ReadSheet(xll, ht,filename);
                tgs.teacherID = identify_teacher(tgs);
                tgs.subject = identify_subject(tgs);
                returnlist.Add(tgs);
            }
            //memo("teachername=" + tgs.teachername);
            //foreach (tgsitemclass tt in tgs.tgsitems)
            //{
            //    memo(tt.print());
            //}

            

            //Cleanup
            xlWorkBook.Close(false, null, null);
            xlApp.Quit();

            foreach (Excel.Worksheet xll in sheetlist)
                Marshal.ReleaseComObject(xll);
            //Marshal.ReleaseComObject(xlhtl);
            Marshal.ReleaseComObject(xlWorkBook);
            Marshal.ReleaseComObject(xlApp);

            return returnlist;
        }

        private string identify_subject(tgssheetclass tgs)
        {
            if ( subjectnamedict.ContainsKey(tgs.subject))
                return subjectnamedict[tgs.subject];

            foreach (string fsub in subjectfolderdict.Keys)
            {
                if ( tgs.filename.Contains(fsub))
                    return subjectfolderdict[fsub];
            }

            return tgs.subject;
        }

        private string identify_teacher_name(string fnamepar, string lnamepar) //returns signature of teacher, if possible
        {
            string fname = fnamepar.ToLower();
            string lname = lnamepar.ToLower();

            var q1 = (from c in db.Teacher where c.Firstname == fname where c.Lastname == lname select c);
            if (q1.Count() > 0)
                return q1.First().TeacherID;

            var q2 = (from c in db.Teacher where c.Lastname == lname select c);
            if ( q2.Count() > 0)
            {
                if (fname.Length == 1)
                {
                    var q3 = (from c in q2.ToList() where c.Firstname.Substring(0, 1) == fname select c);
                    if (q3.Count() == 1)
                        return q3.First().TeacherID;
                }
            }

            return "###";
            //return identify_teacher_name(fname + " " + lname);
        }
        private string identify_teacher_name(string tnamepar) //returns signature of teacher, if possible
        {
            string tname = tnamepar.ToLower();

            var sigquery =
                from teacher in teacherlist
                where teacher.teacherID == tname
                select teacher;
            if (sigquery.Count() == 1)
                return tname;

            if (extra_sigdict.ContainsKey(tname))
                return extra_sigdict[tname];

            var namequery =
                from teacher in teacherlist
                where teacher.name.ToLower() == tname
                select teacher;

            List<teacherclass> tl = namequery.ToList();
            if (tl.Count == 1)
                return tl.First().teacherID;
            else if (tl.Count > 1)
                memo(tname + " multihit namequery");

            string[] nameparts = tname.Split();
            string swapname = "";
            if ( nameparts.Length > 1)
            {
                swapname = nameparts[1] + " " + nameparts[0];
                var swapquery =
                    from teacher in teacherlist
                    where teacher.name.ToLower() == swapname
                    select teacher;

                tl = swapquery.ToList();
                if (tl.Count == 1)
                    return tl.First().teacherID;
                else if (tl.Count > 1)
                    memo(tname + " multihit swapquery");
            }

            var lastnamequery =
                from teacher in teacherlist
                where teacher.lastname.ToLower() == tname
                select teacher;

            tl = lastnamequery.ToList();
            if (tl.Count == 1)
                return tl.First().teacherID;
            else if (tl.Count > 1)
                memo(tname + " multihit lastnamequery");

            var lastnamequery2 =
                from teacher in teacherlist
                where tname.Contains(teacher.lastname.ToLower())
                select teacher;

            tl = lastnamequery2.ToList();
            if (tl.Count == 1)
                return tl.First().teacherID;
            else if (tl.Count > 1)
            {
                //memo(tname + " multihit lastnamequery2");
                int npmatch = 0;
                string npsig = "";
                foreach (teacherclass tt in tl)
                {
                    //memo("   " + tt.name+" "+tt.teacherID);
                    foreach (string np in nameparts)
                    {
                        if ( np == tt.lastname.ToLower())
                        {
                            npmatch++;
                            npsig = tt.teacherID;
                        }
                    }
                }
                if (npmatch == 1)
                    return npsig;
            }

            var firstnamequery =
                from teacher in teacherlist
                where teacher.firstname.ToLower() == tname
                select teacher;

            tl = firstnamequery.ToList();
            if (tl.Count == 1)
                return tl.First().teacherID;
            else if (tl.Count > 1)
                memo(tname + " multihit firstnamequery");

            var subsetquery =
                from teacher in teacherlist
                where (teacher.name.ToLower().Contains(tname) || tname.Contains(teacher.name.ToLower()))
                select teacher;

            tl = subsetquery.ToList();
            if (tl.Count == 1)
                return tl.First().teacherID;
            //else if (tl.Count > 1)
            //    memo(tname + " multihit subsetquery");

            //int dist = LevenshteinDistance(ccname, ti.label.Trim());
            int mindist = 99;
            string sigfound = "###";
            int nfound = 0;
            foreach (teacherclass tc in teacherlist)
            {
                int dist = LevenshteinDistance(tc.name,tname);
                if (dist < mindist)
                {
                    mindist = dist;
                    sigfound = tc.teacherID;
                    nfound = 1;
                }
                else if (dist == mindist)
                {
                    nfound++;
                }
                if ( !String.IsNullOrEmpty(swapname))
                {
                    dist = LevenshteinDistance(tc.name.ToLower(), swapname);
                    if (dist < mindist)
                    {
                        mindist = dist;
                        sigfound = tc.teacherID;
                        nfound = 1;
                    }
                    else if (dist == mindist)
                    {
                        nfound++;
                    }
                }
            }
            if (( mindist < 4 ) && (nfound == 1 ))
            {
                return sigfound;
            }

            //memo(tname + " " + mindist + " " + sigfound);
            return "###";
        }

        private string identify_teacher(tgssheetclass tgs) //returns signature of teacher, if possible
        {

            if ( extra_sigdict.ContainsKey(tgs.teachername.ToLower()))
                return extra_sigdict[tgs.teachername.ToLower()];

            var birthdayquery = 
                from teacher in teacherlist
                where teacher.birthday == tgs.birthday
                select teacher;

            List<teacherclass> tl = birthdayquery.ToList();
            if (tl.Count == 1)
                return tl.First().teacherID;

            return identify_teacher_name(tgs.teachername.ToLower());
        }

        private void identify_best(string acyear, bool ht) //for each teacher, find the best of several versions of the tgs for a specific semester
        {
            memo("Identifying best TGS version for each teacher...");
            string acyearslash = acyear.Replace("-", "/");
            memo("acyear = " + acyear);
            foreach (teacherclass tc in teacherlist)
            {
                var query = from tgs in tgslist
                            where tgs.teacherID == tc.teacherID
                            where tgs.academicyear == acyearslash
                            where tgs.is_ht == ht
                            select tgs;
                List<tgssheetclass> sigtgs = query.ToList();

                if (sigtgs.Count == 0)
                    continue;

                memo("sigtgs.Count = " + sigtgs.Count);
                memo("sigtgs.academicyear = " + sigtgs.First().academicyear);
                if (sigtgs.Count == 1)
                    sigtgs.First().best = true;
                else
                {
                    int ndef = 0;
                    foreach (tgssheetclass tgs in sigtgs)
                        if (tgs.definitive)
                            ndef++;
                    memo("ndef = " + ndef);
                    if (ndef == 1)
                    {
                        foreach (tgssheetclass tgs in sigtgs)
                            if (tgs.definitive)
                                tgs.best = true;
                    }
                    else
                    {
                        bool usedef = (ndef > 0);
                        DateTime newest = DateTime.Now.AddYears(-40);
                        foreach (tgssheetclass tgs in sigtgs)
                            if (tgs.definitive == usedef)
                                if (tgs.modified > newest)
                                    newest = tgs.modified;
                        memo(newest.ToString());
                        foreach (tgssheetclass tgs in sigtgs)
                            if (tgs.definitive == usedef)
                                if (tgs.modified == newest)
                                    tgs.best = true;
                    }
                }

            }
            memo("DONE identifying best TGS version for each teacher.");
        }

        private string getsemesterstring(int tgsyear,bool ht)
        {
            string semesterstring = @"VT-";
            if (ht)
                semesterstring = @"HT-";
            semesterstring += (tgsyear - 2000).ToString();
            return semesterstring;
        }

        private void ReadTGSButton_Click(object sender, EventArgs e)
        {
            tgslist.Clear();
            int nfile = 0;
            int ntgs = 0;
            int ndoubles = 0;
            Dictionary<string, int> subjects = new Dictionary<string, int>();
            string onlywith = tbStartfile.Text;
            List<string> filelist = get_filelist(@"C:\dotnwb3\TGS\");
            foreach (string f in filelist)
            {
                nfile++;
                if (!String.IsNullOrEmpty(onlywith) && !f.Contains(onlywith))
                    continue;
                if (!String.IsNullOrEmpty(academy) && !f.Contains(@"\"+academy+@"\"))
                    continue;

                if ( academy == "IoS")
                {
                    Regex r = new Regex(@"\d\d-\d\d");
                    if ( r.IsMatch(f))
                        if (!f.Contains(academicyear))
                            continue;
                }
                else
                {
                    string semesterstring = @"\DEF " + getsemesterstring(tgsyear, ht) + @"\";
                    if (!f.Contains(semesterstring))
                        continue;
                }

                System.IO.FileInfo finfo = new System.IO.FileInfo(f);

                if (finfo.LastWriteTime.Year < tgsyear)
                    continue;
                if (finfo.LastWriteTime.Year > tgsyear+1)
                    continue;
                //memo(f);
                List<tgssheetclass> flist = ReadTGSFile(f);
                foreach (tgssheetclass tgs in flist)
                {
                    if (tgs.teachername.Contains("###"))
                        continue;
                    ntgs++;
                    tgs.modified = finfo.LastWriteTime;
                    memo(ntgs.ToString() + "\t" + tgs.checksum() + "\t" + f + "\t"+(filelist.Count-nfile).ToString());
                    if (!subjects.ContainsKey(tgs.subject))
                        subjects.Add(tgs.subject, 0);
                    subjects[tgs.subject]++;
                    tgslist.Add(tgs);

                }

                if (nfile > maxfiles)
                    break;
            }
            foreach (string sub in subjects.Keys)
            {
                memo(sub + "\t" + subjects[sub].ToString());
            }
            identify_best(academicyear,ht);
            foreach (tgssheetclass tgs in tgslist)
                if (!tgs.best)
                    ndoubles++;

            memo("ndoubles = " + ndoubles.ToString());

            string outfile = @"C:\dotnwb3\out-" + academy + "-" + getsemesterstring(tgsyear,ht) + "-" + getdatestring() + ".txt";
            using (StreamWriter sw = new StreamWriter(outfile))
            {
                sw.WriteLine(tgssheetclass.tableheader());
                foreach (tgssheetclass tgs in tgslist)
                {
                    if (tgs.best)
                    {
                        sw.WriteLine(tgs.dataline());
                        foreach (tgsitemclass ti in tgs.tgsitems)
                            sw.WriteLine(ti.print());
                    }
                }
            }
            memo("DONE!");
            outfilebutton.Enabled = false;
            ReadTGSButton.Enabled = false;
            db_TGSbutton.Enabled = true;

        }

        //public List<string> getcoursecode(string s)
        //{
        //    List<string> rl = new List<string>();

        //    string regexcode = @"\p{L}{2}\d{1}[xX\?\d]{3}"; //test version! fix real version too!
            
        //    Match m = Regex.Match(s, regexcode, RegexOptions.IgnoreCase);
        //    while (m.Success)
        //    {
        //        rl.Add(m.Groups[0].Value);
        //        memo("match: "+m.Groups[0].Value);
        //        m = m.NextMatch();

        //    }

        //    return rl;
        //}

        public void testbutton_Click(object sender, EventArgs e)
        {
            //read_courseresultfile(@"C:\dotnwb3\kursdata\genomstromning_tot.txt");

            memo("test button click");

            Dictionary<string, string> tsdict = new Dictionary<string, string>();
            tsdict.Add("Zuzana Macuchova", "kulturgeografi");
            tsdict.Add("Yanina Espegren", "företagsekonomi");
            tsdict.Add("Yangfan Hultgren", "kinesiska");
            tsdict.Add("Voicu Brabie", "materialteknik");
            tsdict.Add("Victoria Kihlström", "företagsekonomi");
            tsdict.Add("Wei Hing Rosenkvist", "kinesiska");
            tsdict.Add("Tina Wik", "byggteknik");
            tsdict.Add("Sarah Ramsay", "engelska");
            tsdict.Add("Olga Viberg", "ryska");
            tsdict.Add("Nina Bengtsson", "franska");
            tsdict.Add("Mats Öhlén", "statsvetenskap");
            tsdict.Add("Mats Lundgren", "pedagogik");
            tsdict.Add("Mats Braun", "statsvetenskap");
            tsdict.Add("Mariya Aida Niendorf", "japanska");
            tsdict.Add("Maria Petersson", "naturvetenskap");
            tsdict.Add("Maria Fredriksson Sjöberg", "pedagogiskt arbete");
            tsdict.Add("Malin Roitman", "franska");
            tsdict.Add("Luis Conde-Costas", "sociologi");
            tsdict.Add("Lovisa Sumpter", "matematikdidaktik");
            tsdict.Add("Kumar Babu Surreddi", "materialteknik");
            tsdict.Add("Kristine Ohrem Andersers", "pedagogiskt arbete");
            tsdict.Add("Karl Hansson", "datateknik");
            tsdict.Add("Johan Håkansson", "kulturgeografi");
            tsdict.Add("Jesper Engström", "maskinteknik");
            tsdict.Add("Jennie Vinter", "socialt arbete");
            tsdict.Add("Jennie Svensson", "materialteknik");
            tsdict.Add("Ioanna Farsari", "turism");
            tsdict.Add("Ingela Spegel Nääs", "matematikdidaktik");
            tsdict.Add("Ibro Ribic", "idrotts- och hälsovetenskap");
            tsdict.Add("Hed Kerstin Larsson", "naturvetenskap");
            tsdict.Add("Fredrik Land", "engelska");
            tsdict.Add("Eva-Lena Erixon", "matematikdidaktik");
            tsdict.Add("Eva Stattin", "skogsteknik");
            tsdict.Add("Erik Westholm", "kulturgeografi");
            tsdict.Add("Erica Schytt", "omvårdnad");
            tsdict.Add("Emil Gustafsson", "materialteknik");
            tsdict.Add("Elisabeth Jobs", "medicinsk vetenskap");
            tsdict.Add("David Molnár", "materialteknik");
            tsdict.Add("Daniel Nilsson", "energiteknik");
            tsdict.Add("Daniel Fredriksson", "ljud- och musikproduktion");
            tsdict.Add("Christine Riedwyl Gottberg", "tyska");
            tsdict.Add("Christina Kullberg", "franska");
            tsdict.Add("Catharina Gustavsson", "medicinsk vetenskap");
            tsdict.Add("Caroline Maria Bastholm", "energiteknik");
            tsdict.Add("Bethanne Yoxsimer Paulsrud", "engelska");
            tsdict.Add("Annica Engström", "omvårdnad");
            tsdict.Add("Anneli Strömsöe", "medicinsk vetenskap");
            tsdict.Add("Anna Teledahl", "pedagogik");
            tsdict.Add("Ann Hedlund", "arbetsvetenskap");
            tsdict.Add("Anette Sahlqvist", "omvårdnad");
            tsdict.Add("Anders Törnqvist", "geografi");

            foreach (string tname in tsdict.Keys)
            {
                string tid = identify_teacher_name(tname);
                Teacher tt = (from c in db.Teacher where c.TeacherID == tid select c).FirstOrDefault();
                if (tt == null)
                    continue;
                tt.Subject = tsdict[tname];
                db.SubmitChanges();
            }
            


            var q = (from c in db.Teacher where c.Subject == "ämne saknas" select c);
            var qprofile = (from c in q where c.Profileteacher.Count > 0 select c);
            var qpub = (from c in q where c.Author.Count > 0 select c);
            var qtgs = (from c in q where c.TGS.Count > 0 select c);

            memo("Profile without subject:");
            foreach (Teacher c in qprofile)
                memo(c.Name+"\t"+c.TeacherID);
            memo("Publish without subject:");
            foreach (Teacher c in qpub)
                memo(c.Name + "\t" + c.TeacherID);
            memo("TGS without subject:");
            foreach (Teacher c in qtgs)
                memo(c.Name + "\t" + c.TeacherID);

            //foreach (Programtable pt in db.Programtable)
            //{
            //    double levelsum = 0;
            //    int n = 0;
            //    foreach (Programcourse pc in pt.Programcourse)
            //    {
            //        int level = courselevel(pc.Coursecode);
            //        levelsum += level;
            //        n++;
            //    }
            //    if (n > 0)
            //    {
            //        double average = levelsum / n;
            //        memo(pt.Name + ": " + average.ToString());
            //        if ( average > 2.5)
            //        {
            //            pt.Advanced = true;
            //        }
            //        else
            //        {
            //            pt.Advanced = false;
            //        }
            //    }
            //    db.SubmitChanges();
            //}
            //string s1 = "Detaljhandelsprogrammet  120 hp  (SDEPG)";
            //memo(s1);
            //foreach (string c in tgssheetclass.getapplcode(s1))
            //{
            //    memo(c);
            //}
            //foreach (float c in tgssheetclass.gethp(s1))
            //{
            //    memo(c.ToString());
            //}
            //s1 = "Arabiska dialekter: Syriska IV (H2H98) (ITD 25%) 7.5hp";
            //memo(s1);
            //foreach (string c in tgssheetclass.getapplcode(s1))
            //{
            //    memo(c);
            //}
            //foreach (float c in tgssheetclass.gethp(s1))
            //{
            //    memo(c.ToString());
            //}

            //s1 = "anmälan-linnea-2016-alla.xls";
            //memo(getfileyear(s1).ToString());
            //s1 = "progreg-linnea-T1-2012-ht.txt";
            //memo(getfileyear(s1).ToString());
            //memo("Termin " + getfileprogsemester(s1));
            //s1 = "ht2008-u2-age.txt";
            //memo(getfileyear(s1).ToString());

            //hbookclass course_nmatch = new hbookclass();
            //foreach (tgssheetclass tgs in tgslist)
            //{
            //    foreach (tgsitemclass ti in (from ti in tgs.tgsitems where ti.category == 0 select ti))
            //    {
            //        course_nmatch.Add(identify_course(ti,tgs.year(),tgs.is_ht).Count);
            //    }
            //}

            //memo("Nmatch");
            //memo(course_nmatch.PrintIHist());
            //memo("codematch");
            //memo(course_codematchhist.PrintIHist());
            //memo("bestmatch");
            //memo(course_bestmatchhist.PrintIHist());


            //Dictionary<string, int> dd = new Dictionary<string, int>();
            //foreach (courseclass cc in courselist)
            //{
            //    string s = cc.coursecode.Substring(0, 2);
            //    if (!dd.ContainsKey(s))
            //        dd.Add(s, 0);
            //    dd[s]++;
            //}
            //foreach (string s in dd.Keys)
            //{
            //    var q = from course in courselist
            //            where course.coursecode.Contains(s)
            //            select course;
            //    string example = q.First().name;

            //    List<int> ol = new List<int>();
            //    List<string> sl = new List<string>();

            //    //foreach (tgssheetclass tgs in tgslist)
            //    //{
            //    //    foreach (tgsitemclass ti in tgs.tgsitems)
            //    //    {
            //    //        foreach (string cc in ti.coursecodes)
            //    //        {
            //    //            if (cc.Contains(s))
            //    //            {
            //    //                if ( ti.objekt > 0 )
            //    //                    ol.Add(ti.objekt);

            //    //                sl.Add(tgs.subject);
            //    //            }
            //    //        }
            //    //    }
            //    //}
            //    var q2 = from tgs in tgslist
            //             from item in tgs.tgsitems
            //             where item.coursecodes.Count > 0
            //             where item.coursecodes.First().Contains(s)
            //             where item.objekt > 0
            //             select item.objekt;
            //    ol = q2.ToList();
            //    string obj = "";
            //    if ( ol.Count > 0 )
            //        obj = ol.First().ToString();

            //    var q3 = from tgs in tgslist
            //             from item in tgs.tgsitems
            //             where item.coursecodes.Count > 0
            //             where item.coursecodes.First().Contains(s)
            //             select tgs.subject;
            //    sl = q3.ToList();

            //    string subj = "";
            //    if (sl.Count > 0)
            //        subj = sl.First();

            //    memo(s + "\t" + dd[s] + "\t" + obj + "\t" + subj + "\t\t" + example);
            //}

            //List<string> rl = new List<string>();

            //rl = tgssheetclass.getcoursecode("HH2035");
            //memo(rl.Count.ToString() + " matches");
            //rl = tgssheetclass.getcoursecode("HH 2035");
            //memo(rl.Count.ToString() + " matches");
            //rl = tgssheetclass.getcoursecode("1234567890");
            //memo(rl.Count.ToString() + " matches");
            //rl = tgssheetclass.getcoursecode("HH2035JK1234");
            //memo(rl.Count.ToString() + " matches");
            //rl = tgssheetclass.getcoursecode("HH20xx");
            //memo(rl.Count.ToString() + " matches");
            //rl = tgssheetclass.getcoursecode("HH20xy");
            //memo(rl.Count.ToString() + " matches");
            //rl = tgssheetclass.getcoursecode("HH2x2X");
            //memo(rl.Count.ToString() + " matches");
            //rl = tgssheetclass.getcoursecode("HH20??");
            //memo(rl.Count.ToString() + " matches");

        }

        private void filldicts()
        {
            batchentrydict.Add("Barnmorskeprogrammet", "Barnmorskeprogrammet  90 hp  (VBARA)");
            batchentrydict.Add("Business Intelligence: Magisterprogram", "Business Intelligence: Magisterprogram  60 hp  (DPBIA)");
            batchentrydict.Add("Entreprenöriellt företagande", "Entreprenöriellt företagande  120 hp  (SEFTG)");
            batchentrydict.Add("Film- och TV-produktion", "Kandidatprogrammet - Film- och TV-produktion  180 hp  (KFTVG)");
            batchentrydict.Add("Grundlärarprogrammet grundskolans årskurs 4-6", "Grundlärarprogrammet - Grundskolans årskurs 4-6  240 hp  (LP46A)");
            batchentrydict.Add("Kandidatprogrammet - Manus för film och TV", "Kandidatprogrammet - Manus för film och TV  180 hp  (KMFTG)");
            batchentrydict.Add("Magisterprogram i nationalekonomi", "Magisterprogram i nationalekonomi  60 hp  (SNATA)");
            batchentrydict.Add("Magisterprogram i pedagogiskt arbete", "Magisterprogram i pedagogiskt arbete  60 hp  (LMPAA)");
            batchentrydict.Add("Manus för film och TV", "Kandidatprogrammet - Manus för film och TV  180 hp  (KMFTG)");
            batchentrydict.Add("Masterprogram i metallernas bearbetning", "Masterprogram i metallernas bearbetning  120 hp  (TMBBA)");
            batchentrydict.Add("Musik- och ljuddesign", "Musik- och ljuddesign  120 hp  (KMLUG)");
            batchentrydict.Add("Specialistsjuksköterskeprogram inom vård av äldre", "Specialistsjuksköterskeprogram inom vård av äldre  60 hp  (VSPÄA)");
            batchentrydict.Add("Teknisk/Naturvetenskaplig bastermin", "Teknisk/Naturvetenskaplig bastermin  30 hp  (TTNVB)");
            batchentrydict.Add("Yrkeslärarprogrammet", "Yrkeslärarprogrammet  90 hp  (LYRKA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Bild - nätbaserad utbildning", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Engelska - Historia", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Engelska - nätbaserad utbildning", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Engelska - Religion", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Engelska - Svenska som andraspråk", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Franska - Engelska", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Franska - nätbaserad utbildning", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Historia - nätbaserad utbildning", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Historia - Religion", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Historia", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Idrott och Hälsa", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Matematik", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Religion - Historia", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Religion - nätbaserad utbildning", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Spanska - Engelska", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Spanska - nätbaserad utbildning", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Svenska - Engelska", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Svenska - Franska", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Svenska - nätbaserad utbildning", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Svenska - Religion", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Svenska - Spanska", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Svenska - Svenska som andraspråk", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Svenska - Tyska", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Svenska -Historia", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Svenska som andraspråk - Engelska", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Svenska som andraspråk", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Svenska", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Tyska - Engelska", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");
            batchentrydict.Add("Ämneslärarprogrammet - inriktning gymnasieskolan - Tyska - nätbaserad utbildning", "Ämneslärarprogrammet - inriktning gymnasieskolan  330 hp  (LAGYA)");

            progcodedict.Add("DDOEG", "DBROG");
            progcodedict.Add("DIGEG", "DBROG");
            progcodedict.Add("DFITG", "DBROG");
            progcodedict.Add("DSYPG", "DSYSG");
            progcodedict.Add("HAFSA", "HAFRA");
            progcodedict.Add("HEIIA", "HEILA");
            progcodedict.Add("HESPA", "HENEA");
            progcodedict.Add("KMFTG", "KFRIG");
            progcodedict.Add("KMTVG", "KFRIG");
            progcodedict.Add("KFTVG", "KFTPG");
            progcodedict.Add("KMLUG", "KLUDG");
            progcodedict.Add("KMULG", "KLUDG");
            progcodedict.Add("ULPBP", "LARAG");
            progcodedict.Add("SDETG", "SDEPG");
            progcodedict.Add("SREMG", "SDHPG");
            progcodedict.Add("SMDTA", "SDUVA");
            progcodedict.Add("SEMPG", "SEKPG");
            progcodedict.Add("SHVPG", "SHALG");
            progcodedict.Add("SSAMG", "SHALG");
            progcodedict.Add("SPEAG", "SPAPG");
            progcodedict.Add("SPERG", "SPAPG");
            progcodedict.Add("SSMTG", "SPORG");
            progcodedict.Add("SPMAG", "SPORG");
            progcodedict.Add("SITUG", "STAGG");
            progcodedict.Add("TBYLG", "TBALG");
            progcodedict.Add("TBTEG", "TBBYG");
            progcodedict.Add("TETPG", "TENEG");
            progcodedict.Add("TETHG", "TENGG");
            progcodedict.Add("TGDKG", "TGDEG");
            progcodedict.Add("TMIPG", "TMATG");
            progcodedict.Add("TMAHG", "TMATG");
            progcodedict.Add("TFORA", "TMBBA");
            progcodedict.Add("TMSTA", "TMSEA");
            progcodedict.Add("TMTHG", "TMULG");
            progcodedict.Add("TMPKG", "TMULG");
            progcodedict.Add("TSETA", "TSENA");
            progcodedict.Add("TSUNA", "TSENA");
            progcodedict.Add("TTNBB", "TTBNB");
            progcodedict.Add("TTBAB", "TTBNB");
            progcodedict.Add("VBARA", "VBAPA");
            progcodedict.Add("VBPRA", "VBAPA");
            progcodedict.Add("VSDAA", "VDISA");
            progcodedict.Add("VSDSA", "VDISA");
            progcodedict.Add("VPYVA", "VPSYA");
            progcodedict.Add("VSSPA", "VPSYA");
            progcodedict.Add("VSSPG", "VSJUG");
            progcodedict.Add("TSONA", "TSENA");
            progcodedict.Add("DDISG", "DBROG");
            progcodedict.Add("TBDBP", "DBROG");
            progcodedict.Add("ASYBP", "DSYSG");
            progcodedict.Add("HIRLA", "HEILA");
            progcodedict.Add("UMEPP", "HELIA");
            progcodedict.Add("HENLA", "HELIA");
            progcodedict.Add("HESLG", "HENKG");
            progcodedict.Add("KMBRG", "KFRIG");
            progcodedict.Add("KMMBG", "KFRIG");
            progcodedict.Add("KLMPG", "KSMPG");
            progcodedict.Add("SAKIG", "SDHPG");
            progcodedict.Add("SDUTA", "SDUVA");
            progcodedict.Add("SENFG", "SEFTG");
            progcodedict.Add("SEKOG", "SEKPG");
            progcodedict.Add("SAMVG", "SHALG");
            progcodedict.Add("SINTG", "STAGG");
            progcodedict.Add("TBYAG", "TBALG");
            progcodedict.Add("TMVIG", "TBBAG");
            progcodedict.Add("TBMVG", "TBBAG");
            progcodedict.Add("TBYGG", "TBBYG");
            progcodedict.Add("THBEG", "TBMMG");
            progcodedict.Add("TPVYP", "TEKPG");
            progcodedict.Add("TPTYP", "TEKPG");
            progcodedict.Add("TPTOG", "TEKPG");
            progcodedict.Add("TENTG", "TENEG");
            progcodedict.Add("TGRAG", "TGDEG");
            progcodedict.Add("KGRDG", "TGDEG");
            progcodedict.Add("TMATA", "TMDCP");
            progcodedict.Add("TMDEA", "TMDCP");
            progcodedict.Add("TIULG", "TMULG");
            progcodedict.Add("TMIUG", "TMULG");
            progcodedict.Add("TOENA", "TSENA");
            progcodedict.Add("TBASB", "TTEBB");
            progcodedict.Add("FBÅLL", "TTEBB");
            progcodedict.Add("KMEDG", "UMKBP");
            progcodedict.Add("KIMPG", "UMKBP");
            progcodedict.Add("KPPRG", "UMKBP");
            progcodedict.Add("VSVÅA", "VSPÄA");

            progsubjdict.Add("HAFRA", "african studies");
            progsubjdict.Add("HAFMA", "african studies");
            progsubjdict.Add("KFTPG", "bildproduktion");
            progsubjdict.Add("KFRIG", "bildproduktion");
            progsubjdict.Add("TBALG", "byggteknik");
            progsubjdict.Add("TBBYG", "byggteknik");
            progsubjdict.Add("TBBAG", "byggteknik");
            progsubjdict.Add("TBAIG", "byggteknik");
            progsubjdict.Add("TBYIG", "byggteknik");
            progsubjdict.Add("DBROG", "datateknik");
            progsubjdict.Add("TENGG", "energiteknik");
            progsubjdict.Add("TENEG", "energiteknik");
            progsubjdict.Add("TSENA", "energiteknik");
            progsubjdict.Add("TMSEA", "energiteknik");
            progsubjdict.Add("HENEA", "engelska");
            progsubjdict.Add("HEILA", "engelska");
            progsubjdict.Add("HELIA", "engelska");
            progsubjdict.Add("SEFTG", "entreprenörskap");
            progsubjdict.Add("SDEPG", "företagsekonomi");
            progsubjdict.Add("SDHPG", "företagsekonomi");
            progsubjdict.Add("SEKPG", "företagsekonomi");
            progsubjdict.Add("SBUSA", "företagsekonomi");
            progsubjdict.Add("SPORG", "företagsekonomi");
            progsubjdict.Add("KDADG", "grafisk teknologi");
            progsubjdict.Add("TGDEG", "grafisk teknologi");
            progsubjdict.Add("KDTMG", "grafisk teknologi");
            progsubjdict.Add("VITHG", "idrotts- och hälsovetenskap");
            progsubjdict.Add("VITPG", "idrotts- och hälsovetenskap");
            progsubjdict.Add("VMIFA", "idrotts- och hälsovetenskap");
            progsubjdict.Add("VTRPG", "idrotts- och hälsovetenskap");
            progsubjdict.Add("TIEHG", "industriell ekonomi");
            progsubjdict.Add("DSYSG", "informatik");
            progsubjdict.Add("DUETG", "informatik");
            progsubjdict.Add("KAVIG", "ljud- och musikproduktion");
            progsubjdict.Add("KSMPG", "ljud- och musikproduktion");
            progsubjdict.Add("HMASA", "ljud- och musikproduktion");
            progsubjdict.Add("KLUDG", "ljud- och musikproduktion");
            progsubjdict.Add("TMULG", "maskinteknik");
            progsubjdict.Add("TEKPG", "maskinteknik");
            progsubjdict.Add("TOIHG", "maskinteknik");
            progsubjdict.Add("TTNVB", "matematik");
            progsubjdict.Add("TNVTB", "matematik");
            progsubjdict.Add("TTEBB", "matematik");
            progsubjdict.Add("TTBNB", "matematik");
            progsubjdict.Add("TMDCP", "materialteknik");
            progsubjdict.Add("TMATG", "materialteknik");
            progsubjdict.Add("TMBBA", "materialteknik");
            progsubjdict.Add("DPBIA", "mikrodata");
            progsubjdict.Add("DMBIA", "mikrodata");
            progsubjdict.Add("SNATA", "nationalekonomi");
            progsubjdict.Add("STUEA", "nationalekonomi");
            progsubjdict.Add("VSJUG", "omvårdnad");
            progsubjdict.Add("VSPÄA", "omvårdnad");
            progsubjdict.Add("VPSYA", "omvårdnad");
            progsubjdict.Add("VDISA", "omvårdnad");
            progsubjdict.Add("SPAPG", "pal");
            progsubjdict.Add("LFORG", "pedagogiskt arbete");
            progsubjdict.Add("LG13A", "pedagogiskt arbete");
            progsubjdict.Add("LP46A", "pedagogiskt arbete");
            progsubjdict.Add("LKGYA", "pedagogiskt arbete");
            progsubjdict.Add("LK79A", "pedagogiskt arbete");
            progsubjdict.Add("LARAG", "pedagogiskt arbete");
            progsubjdict.Add("LMPAA", "pedagogiskt arbete");
            progsubjdict.Add("LVALG", "pedagogiskt arbete");
            progsubjdict.Add("LYRKA", "pedagogiskt arbete");
            progsubjdict.Add("LA79A", "pedagogiskt arbete");
            progsubjdict.Add("LAGYA", "pedagogiskt arbete");
            progsubjdict.Add("HRVPA", "religionsvetenskap");
            progsubjdict.Add("HRMMA", "religionsvetenskap");
            progsubjdict.Add("TBOSG", "samhällsbyggnadsteknik");
            progsubjdict.Add("VESAA", "socialt arbete");
            progsubjdict.Add("VSOAG", "socialt arbete");
            progsubjdict.Add("VSOCG", "socialt arbete");
            progsubjdict.Add("VBAPA", "srph");
            progsubjdict.Add("SHALG", "statsvetenskap");
            progsubjdict.Add("HSSAA", "svenska som andraspråk");
            progsubjdict.Add("STAGG", "turism");
            progsubjdict.Add("SDUVA", "turism");
            progsubjdict.Add("TBMMG", "byggteknik");
            progsubjdict.Add("DATIA", "datateknik");
            progsubjdict.Add("HENKG", "engelska");
            progsubjdict.Add("AEKMP", "företagsekonomi");
            progsubjdict.Add("SIBMG", "företagsekonomi");
            progsubjdict.Add("UHKBP", "historia");
            progsubjdict.Add("VIDOG", "idrotts- och hälsovetenskap");
            progsubjdict.Add("AETYP", "informatik");
            progsubjdict.Add("KLJUG", "ljud- och musikproduktion");
            progsubjdict.Add("UMKBP", "ljud- och musikproduktion");
            progsubjdict.Add("UMPBP", "ljud- och musikproduktion");
            progsubjdict.Add("UGRBP", "pedagogiskt arbete");
            progsubjdict.Add("UGSBP", "pedagogiskt arbete");
            progsubjdict.Add("UGYBP", "pedagogiskt arbete");
            progsubjdict.Add("ULGYP", "pedagogiskt arbete");
            progsubjdict.Add("HREMA", "religionsvetenskap");
            progsubjdict.Add("HSOCA", "sociologi");
            progsubjdict.Add("HEUPA", "sociologi");

            progleveldict.Add("HAFRA", "avancerad");
            progleveldict.Add("HAFMA", "avancerad");
            progleveldict.Add("KFTPG", "grund");
            progleveldict.Add("KFRIG", "grund");
            progleveldict.Add("TBALG", "grund");
            progleveldict.Add("TBBYG", "grund");
            progleveldict.Add("TBBAG", "grund");
            progleveldict.Add("TBAIG", "grund");
            progleveldict.Add("TBYIG", "grund");
            progleveldict.Add("DBROG", "grund");
            progleveldict.Add("TENGG", "grund");
            progleveldict.Add("TENEG", "grund");
            progleveldict.Add("TSENA", "avancerad");
            progleveldict.Add("TMSEA", "avancerad");
            progleveldict.Add("HENEA", "avancerad");
            progleveldict.Add("HEILA", "avancerad");
            progleveldict.Add("HELIA", "avancerad");
            progleveldict.Add("SEFTG", "grund");
            progleveldict.Add("SDEPG", "grund");
            progleveldict.Add("SDHPG", "grund");
            progleveldict.Add("SEKPG", "grund");
            progleveldict.Add("SBUSA", "avancerad");
            progleveldict.Add("SPORG", "grund");
            progleveldict.Add("KDADG", "grund");
            progleveldict.Add("TGDEG", "grund");
            progleveldict.Add("KDTMG", "grund");
            progleveldict.Add("VITHG", "grund");
            progleveldict.Add("VITPG", "grund");
            progleveldict.Add("VMIFA", "avancerad");
            progleveldict.Add("VTRPG", "grund");
            progleveldict.Add("TIEHG", "grund");
            progleveldict.Add("DSYSG", "grund");
            progleveldict.Add("DUETG", "grund");
            progleveldict.Add("KAVIG", "grund");
            progleveldict.Add("KSMPG", "grund");
            progleveldict.Add("HMASA", "avancerad");
            progleveldict.Add("KLUDG", "grund");
            progleveldict.Add("TMULG", "grund");
            progleveldict.Add("TEKPG", "grund");
            progleveldict.Add("TOIHG", "grund");
            progleveldict.Add("TTNVB", "preparand");
            progleveldict.Add("TNVTB", "preparand");
            progleveldict.Add("TTEBB", "preparand");
            progleveldict.Add("TTBNB", "preparand");
            progleveldict.Add("TMDCP", "grund");
            progleveldict.Add("TMATG", "grund");
            progleveldict.Add("TMBBA", "avancerad");
            progleveldict.Add("DPBIA", "avancerad");
            progleveldict.Add("DMBIA", "avancerad");
            progleveldict.Add("SNATA", "avancerad");
            progleveldict.Add("STUEA", "avancerad");
            progleveldict.Add("VSJUG", "grund");
            progleveldict.Add("VSPÄA", "avancerad");
            progleveldict.Add("VPSYA", "avancerad");
            progleveldict.Add("VDISA", "avancerad");
            progleveldict.Add("SPAPG", "grund");
            progleveldict.Add("LFORG", "grund");
            progleveldict.Add("LG13A", "grund");
            progleveldict.Add("LP46A", "grund");
            progleveldict.Add("LKGYA", "avancerad");
            progleveldict.Add("LK79A", "avancerad");
            progleveldict.Add("LARAG", "grund");
            progleveldict.Add("LMPAA", "avancerad");
            progleveldict.Add("LVALG", "grund");
            progleveldict.Add("LYRKA", "grund");
            progleveldict.Add("LA79A", "grund");
            progleveldict.Add("LAGYA", "grund");
            progleveldict.Add("HRVPA", "avancerad");
            progleveldict.Add("HRMMA", "avancerad");
            progleveldict.Add("TBOSG", "grund");
            progleveldict.Add("VESAA", "avancerad");
            progleveldict.Add("VSOAG", "grund");
            progleveldict.Add("VSOCG", "grund");
            progleveldict.Add("VBAPA", "avancerad");
            progleveldict.Add("SHALG", "grund");
            progleveldict.Add("HSSAA", "avancerad");
            progleveldict.Add("STAGG", "grund");
            progleveldict.Add("SDUVA", "avancerad");
            progleveldict.Add("TBMMG", "grund");
            progleveldict.Add("DATIA", "avancerad");
            progleveldict.Add("HENKG", "grund");
            progleveldict.Add("AEKMP", "grund");
            progleveldict.Add("SIBMG", "grund");
            progleveldict.Add("UHKBP", "grund");
            progleveldict.Add("VIDOG", "grund");
            progleveldict.Add("AETYP", "preparand");
            progleveldict.Add("KLJUG", "grund");
            progleveldict.Add("UMKBP", "grund");
            progleveldict.Add("UMPBP", "grund");
            progleveldict.Add("UGRBP", "grund");
            progleveldict.Add("UGSBP", "grund");
            progleveldict.Add("UGYBP", "grund");
            progleveldict.Add("ULGYP", "grund");
            progleveldict.Add("HREMA", "avancerad");
            progleveldict.Add("HSOCA", "avancerad");
            progleveldict.Add("HEUPA", "avancerad");


            academyclass ac37 = new academyclass(); ac37.acid = "ADM"; ac37.label = "Administration"; acdict.Add(37, ac37);
            academyclass ac43 = new academyclass(); ac43.acid = "HM"; ac43.label = "Akademi Humaniora och medier"; acdict.Add(43, ac43);
            academyclass ac46 = new academyclass(); ac46.acid = "IoS"; ac46.label = "Akademi Industri och Samhälle"; acdict.Add(46, ac46);
            academyclass ac45 = new academyclass(); ac45.acid = "UHS"; ac45.label = "Akademi Utbildning, Hälsa och Samhälle"; acdict.Add(45, ac45);
            academyclass ac48 = new academyclass(); ac48.acid = "BIB"; ac48.label = "Bibliotek"; acdict.Add(48, ac48);
            academyclass ac38 = new academyclass(); ac38.acid = "DC"; ac38.label = "DalaCampus"; acdict.Add(38, ac38);
            academyclass ac39 = new academyclass(); ac39.acid = "Gem"; ac39.label = "Gemensamt"; acdict.Add(39, ac39);
            academyclass ac35 = new academyclass(); ac35.acid = "Led"; ac35.label = "Styrelse o rektor"; acdict.Add(35, ac35);
            academyclass ac33 = new academyclass(); ac33.acid = "UFK"; ac33.label = "UFK"; acdict.Add(33, ac33);
            academyclass ac0 = new academyclass(); ac0.acid = "???"; ac0.label = "Okänd akademi"; acdict.Add(0, ac0);


            teacherclass t1 = new teacherclass(); t1.name = "Anna Anåker"; t1.firstname = "Anna"; t1.lastname = "Anåker"; t1.teacherID = "aaa"; t1.birthday = "740731"; teacherlist.Add(t1);
            teacherclass t2 = new teacherclass(); t2.name = "Anna Cristina Åberg"; t2.firstname = "Anna Cristina"; t2.lastname = "Åberg"; t2.teacherID = "aab"; t2.birthday = "560119"; teacherlist.Add(t2);
            teacherclass t3 = new teacherclass(); t3.name = "Anja Achouiantz Hedqvist"; t3.firstname = "Anja"; t3.lastname = "Achouiantz Hedqvist"; t3.teacherID = "aac"; t3.birthday = "750901"; teacherlist.Add(t3);
            teacherclass t4 = new teacherclass(); t4.name = "Anita Andersson"; t4.firstname = "Anita"; t4.lastname = "Andersson"; t4.teacherID = "aad"; t4.birthday = "490519"; teacherlist.Add(t4);
            teacherclass t5 = new teacherclass(); t5.name = "Anna Annerberg"; t5.firstname = "Anna"; t5.lastname = "Annerberg"; t5.teacherID = "aae"; t5.birthday = "721209"; teacherlist.Add(t5);
            teacherclass t6 = new teacherclass(); t6.name = "Alexander Ahlstedt"; t6.firstname = "Alexander"; t6.lastname = "Ahlstedt"; t6.teacherID = "aah"; t6.birthday = "890913"; teacherlist.Add(t6);
            teacherclass t7 = new teacherclass(); t7.name = "Annelie Ädel"; t7.firstname = "Annelie"; t7.lastname = "Ädel"; t7.teacherID = "aal"; t7.birthday = "711129"; teacherlist.Add(t7);
            teacherclass t8 = new teacherclass(); t8.name = "Ann-Marie Look"; t8.firstname = "Ann-Marie"; t8.lastname = "Look"; t8.teacherID = "aam"; t8.birthday = "581022"; teacherlist.Add(t8);
            teacherclass t9 = new teacherclass(); t9.name = "Anneli Andersson"; t9.firstname = "Anneli"; t9.lastname = "Andersson"; t9.teacherID = "aan"; t9.birthday = "590705"; teacherlist.Add(t9);
            teacherclass t10 = new teacherclass(); t10.name = "Anders Arnqvist"; t10.firstname = "Anders"; t10.lastname = "Arnqvist"; t10.teacherID = "aaq"; t10.birthday = "530808"; teacherlist.Add(t10);
            teacherclass t11 = new teacherclass(); t11.name = "Alfonso Arocha"; t11.firstname = "Alfonso"; t11.lastname = "Arocha"; t11.teacherID = "aar"; t11.birthday = "680816"; teacherlist.Add(t11);
            teacherclass t12 = new teacherclass(); t12.name = "Ann-Catrin Andersson"; t12.firstname = "Ann-Catrin"; t12.lastname = "Andersson"; t12.teacherID = "aas"; t12.birthday = "730502"; teacherlist.Add(t12);
            teacherclass t13 = new teacherclass(); t13.name = "Alessandro Aresti"; t13.firstname = "Alessandro"; t13.lastname = "Aresti"; t13.teacherID = "aat"; t13.birthday = "791165"; teacherlist.Add(t13);
            teacherclass t14 = new teacherclass(); t14.name = "Anders Avdic"; t14.firstname = "Anders"; t14.lastname = "Avdic"; t14.teacherID = "aav"; t14.birthday = "501019"; teacherlist.Add(t14);
            teacherclass t15 = new teacherclass(); t15.name = "Astrid Widén-Alnås"; t15.firstname = "Astrid"; t15.lastname = "Widén-Alnås"; t15.teacherID = "aaw"; t15.birthday = "530923"; teacherlist.Add(t15);
            teacherclass t16 = new teacherclass(); t16.name = "Åsa Bartholdsson"; t16.firstname = "Åsa"; t16.lastname = "Bartholdsson"; t16.teacherID = "aba"; t16.birthday = "630914"; teacherlist.Add(t16);
            teacherclass t17 = new teacherclass(); t17.name = "Anders Bergström"; t17.firstname = "Anders"; t17.lastname = "Bergström"; t17.teacherID = "abe"; t17.birthday = "680904"; teacherlist.Add(t17);
            teacherclass t18 = new teacherclass(); t18.name = "Anna Bulgakova"; t18.firstname = "Anna"; t18.lastname = "Bulgakova"; t18.teacherID = "abg"; t18.birthday = "811103"; teacherlist.Add(t18);
            teacherclass t19 = new teacherclass(); t19.name = "Andreas Lagerkvist (Blixt)"; t19.firstname = "Andreas"; t19.lastname = "Lagerkvist (Blixt)"; t19.teacherID = "abi"; t19.birthday = "810508"; teacherlist.Add(t19);
            teacherclass t20 = new teacherclass(); t20.name = "Anna-Britta Larsson"; t20.firstname = "Anna-Britta"; t20.lastname = "Larsson"; t20.teacherID = "abl"; t20.birthday = "481209"; teacherlist.Add(t20);
            teacherclass t21 = new teacherclass(); t21.name = "Annelie Bergman"; t21.firstname = "Annelie"; t21.lastname = "Bergman"; t21.teacherID = "abm"; t21.birthday = "670916"; teacherlist.Add(t21);
            teacherclass t22 = new teacherclass(); t22.name = "Anders Bornhäll"; t22.firstname = "Anders"; t22.lastname = "Bornhäll"; t22.teacherID = "abn"; t22.birthday = "841223"; teacherlist.Add(t22);
            teacherclass t23 = new teacherclass(); t23.name = "Annika Blomqvist"; t23.firstname = "Annika"; t23.lastname = "Blomqvist"; t23.teacherID = "abo"; t23.birthday = "800909"; teacherlist.Add(t23);
            teacherclass t24 = new teacherclass(); t24.name = "Anneli Bergström"; t24.firstname = "Anneli"; t24.lastname = "Bergström"; t24.teacherID = "abs"; t24.birthday = "641223"; teacherlist.Add(t24);
            teacherclass t25 = new teacherclass(); t25.name = "Andrew Casson"; t25.firstname = "Andrew"; t25.lastname = "Casson"; t25.teacherID = "aca"; t25.birthday = "520925"; teacherlist.Add(t25);
            teacherclass t26 = new teacherclass(); t26.name = "André Candell"; t26.firstname = "André"; t26.lastname = "Candell"; t26.teacherID = "acd"; t26.birthday = "940828"; teacherlist.Add(t26);
            teacherclass t27 = new teacherclass(); t27.name = "Ann Catrine Eldh"; t27.firstname = "Ann Catrine"; t27.lastname = "Eldh"; t27.teacherID = "ace"; t27.birthday = "650919"; teacherlist.Add(t27);
            teacherclass t28 = new teacherclass(); t28.name = "Catharina Gustavsson"; t28.firstname = "Catharina"; t28.lastname = "Gustavsson"; t28.teacherID = "acg"; t28.birthday = "611024"; teacherlist.Add(t28);
            teacherclass t29 = new teacherclass(); t29.name = "Anna-Carin Jonsson"; t29.firstname = "Anna-Carin"; t29.lastname = "Jonsson"; t29.teacherID = "acj"; t29.birthday = "681120"; teacherlist.Add(t29);
            teacherclass t30 = new teacherclass(); t30.name = "Anders Claesson"; t30.firstname = "Anders"; t30.lastname = "Claesson"; t30.teacherID = "acl"; t30.birthday = "460201"; teacherlist.Add(t30);
            teacherclass t31 = new teacherclass(); t31.name = "Åsa Dahlstrand"; t31.firstname = "Åsa"; t31.lastname = "Dahlstrand"; t31.teacherID = "adh"; t31.birthday = "681108"; teacherlist.Add(t31);
            teacherclass t32 = new teacherclass(); t32.name = "Alex Dos Santos Pruth"; t32.firstname = "Alex Dos"; t32.lastname = "Santos Pruth"; t32.teacherID = "ads"; t32.birthday = "750425"; teacherlist.Add(t32);
            teacherclass t33 = new teacherclass(); t33.name = "Ann-Louise Ebberstein"; t33.firstname = "Ann-Louise"; t33.lastname = "Ebberstein"; t33.teacherID = "aeb"; t33.birthday = "610513"; teacherlist.Add(t33);
            teacherclass t34 = new teacherclass(); t34.name = "Åke Eldsäter"; t34.firstname = "Åke"; t34.lastname = "Eldsäter"; t34.teacherID = "aed"; t34.birthday = "430401"; teacherlist.Add(t34);
            teacherclass t35 = new teacherclass(); t35.name = "Alexandra Eilegård Wallin"; t35.firstname = "Alexandra"; t35.lastname = "Eilegård Wallin"; t35.teacherID = "aee"; t35.birthday = "750526"; teacherlist.Add(t35);
            teacherclass t36 = new teacherclass(); t36.name = "Anna Ehrenberg"; t36.firstname = "Anna"; t36.lastname = "Ehrenberg"; t36.teacherID = "aeh"; t36.birthday = "560117"; teacherlist.Add(t36);
            teacherclass t37 = new teacherclass(); t37.name = "Annicka Elnäs"; t37.firstname = "Annicka"; t37.lastname = "Elnäs"; t37.teacherID = "ael"; t37.birthday = "491019"; teacherlist.Add(t37);
            teacherclass t38 = new teacherclass(); t38.name = "Anna Emmoth"; t38.firstname = "Anna"; t38.lastname = "Emmoth"; t38.teacherID = "aem"; t38.birthday = "700415"; teacherlist.Add(t38);
            teacherclass t39 = new teacherclass(); t39.name = "Annica Engström"; t39.firstname = "Annica"; t39.lastname = "Engström"; t39.teacherID = "aen"; t39.birthday = "671013"; teacherlist.Add(t39);
            teacherclass t40 = new teacherclass(); t40.name = "Anja Eriksson"; t40.firstname = "Anja"; t40.lastname = "Eriksson"; t40.teacherID = "aer"; t40.birthday = "650603"; teacherlist.Add(t40);
            teacherclass t41 = new teacherclass(); t41.name = "Annika Eriksson-Minninger"; t41.firstname = "Annika"; t41.lastname = "Eriksson-Minninger"; t41.teacherID = "aes"; t41.birthday = "590213"; teacherlist.Add(t41);
            teacherclass t42 = new teacherclass(); t42.name = "Amanda Frank"; t42.firstname = "Amanda"; t42.lastname = "Frank"; t42.teacherID = "afa"; t42.birthday = "780116"; teacherlist.Add(t42);
            teacherclass t43 = new teacherclass(); t43.name = "Anna Fjällbäck"; t43.firstname = "Anna"; t43.lastname = "Fjällbäck"; t43.teacherID = "afj"; t43.birthday = "531104"; teacherlist.Add(t43);
            teacherclass t44 = new teacherclass(); t44.name = "Anders Forsman"; t44.firstname = "Anders"; t44.lastname = "Forsman"; t44.teacherID = "afm"; t44.birthday = "630316"; teacherlist.Add(t44);
            teacherclass t45 = new teacherclass(); t45.name = "Anna Fors"; t45.firstname = "Anna"; t45.lastname = "Fors"; t45.teacherID = "afo"; t45.birthday = "680219"; teacherlist.Add(t45);
            teacherclass t46 = new teacherclass(); t46.name = "Anneli Fjordevik"; t46.firstname = "Anneli"; t46.lastname = "Fjordevik"; t46.teacherID = "afr"; t46.birthday = "720616"; teacherlist.Add(t46);
            teacherclass t47 = new teacherclass(); t47.name = "Annika Forsberg"; t47.firstname = "Annika"; t47.lastname = "Forsberg"; t47.teacherID = "afs"; t47.birthday = "780301"; teacherlist.Add(t47);
            teacherclass t48 = new teacherclass(); t48.name = "Anders Gårdestig"; t48.firstname = "Anders"; t48.lastname = "Gårdestig"; t48.teacherID = "aga"; t48.birthday = "760907"; teacherlist.Add(t48);
            teacherclass t49 = new teacherclass(); t49.name = "Annika Gabrils"; t49.firstname = "Annika"; t49.lastname = "Gabrils"; t49.teacherID = "agb"; t49.birthday = "620520"; teacherlist.Add(t49);
            teacherclass t50 = new teacherclass(); t50.name = "Aina Grundström"; t50.firstname = "Aina"; t50.lastname = "Grundström"; t50.teacherID = "agd"; t50.birthday = "581204"; teacherlist.Add(t50);
            teacherclass t51 = new teacherclass(); t51.name = "Åsa Grek"; t51.firstname = "Åsa"; t51.lastname = "Grek"; t51.teacherID = "age"; t51.birthday = "890727"; teacherlist.Add(t51);
            teacherclass t52 = new teacherclass(); t52.name = "Annica Gustafsson"; t52.firstname = "Annica"; t52.lastname = "Gustafsson"; t52.teacherID = "agf"; t52.birthday = "860222"; teacherlist.Add(t52);
            teacherclass t53 = new teacherclass(); t53.name = "Andreas Gröning"; t53.firstname = "Andreas"; t53.lastname = "Gröning"; t53.teacherID = "agg"; t53.birthday = "810202"; teacherlist.Add(t53);
            teacherclass t54 = new teacherclass(); t54.name = "Alessandra Giglio"; t54.firstname = "Alessandra"; t54.lastname = "Giglio"; t54.teacherID = "agi"; t54.birthday = "821271"; teacherlist.Add(t54);
            teacherclass t55 = new teacherclass(); t55.name = "Agneta Sundberg"; t55.firstname = "Agneta"; t55.lastname = "Sundberg"; t55.teacherID = "agn"; t55.birthday = "590107"; teacherlist.Add(t55);
            teacherclass t56 = new teacherclass(); t56.name = "Anton Grenholm"; t56.firstname = "Anton"; t56.lastname = "Grenholm"; t56.teacherID = "agr"; t56.birthday = "721016"; teacherlist.Add(t56);
            teacherclass t57 = new teacherclass(); t57.name = "Amr Sabet"; t57.firstname = "Amr"; t57.lastname = "Sabet"; t57.teacherID = "ags"; t57.birthday = "571118"; teacherlist.Add(t57);
            teacherclass t58 = new teacherclass(); t58.name = "Anna Grundström"; t58.firstname = "Anna"; t58.lastname = "Grundström"; t58.teacherID = "agt"; t58.birthday = "851215"; teacherlist.Add(t58);
            teacherclass t59 = new teacherclass(); t59.name = "Ann Gustafsson"; t59.firstname = "Ann"; t59.lastname = "Gustafsson"; t59.teacherID = "agu"; t59.birthday = "711224"; teacherlist.Add(t59);
            teacherclass t60 = new teacherclass(); t60.name = "Forsberg Anna Hillman"; t60.firstname = "Forsberg Anna"; t60.lastname = "Hillman"; t60.teacherID = "ahb"; t60.birthday = "680712"; teacherlist.Add(t60);
            teacherclass t61 = new teacherclass(); t61.name = "Christian Hecht"; t61.firstname = "Christian"; t61.lastname = "Hecht"; t61.teacherID = "ahc"; t61.birthday = "580421"; teacherlist.Add(t61);
            teacherclass t62 = new teacherclass(); t62.name = "Ann Hedlund"; t62.firstname = "Ann"; t62.lastname = "Hedlund"; t62.teacherID = "ahd"; t62.birthday = "640208"; teacherlist.Add(t62);
            teacherclass t63 = new teacherclass(); t63.name = "Annette Henning"; t63.firstname = "Annette"; t63.lastname = "Henning"; t63.teacherID = "ahe"; t63.birthday = "520212"; teacherlist.Add(t63);
            teacherclass t64 = new teacherclass(); t64.name = "Agnetha Holmgren"; t64.firstname = "Agnetha"; t64.lastname = "Holmgren"; t64.teacherID = "ahg"; t64.birthday = "721104"; teacherlist.Add(t64);
            teacherclass t65 = new teacherclass(); t65.name = "Anna Maria Hipkiss"; t65.firstname = "Anna Maria"; t65.lastname = "Hipkiss"; t65.teacherID = "ahi"; t65.birthday = "731120"; teacherlist.Add(t65);
            teacherclass t66 = new teacherclass(); t66.name = "Anders Henriksson"; t66.firstname = "Anders"; t66.lastname = "Henriksson"; t66.teacherID = "ahk"; t66.birthday = "620406"; teacherlist.Add(t66);
            teacherclass t67 = new teacherclass(); t67.name = "Asuka Hanada"; t67.firstname = "Asuka"; t67.lastname = "Hanada"; t67.teacherID = "ahn"; t67.birthday = "870322"; teacherlist.Add(t67);
            teacherclass t68 = new teacherclass(); t68.name = "Annelie Hoff"; t68.firstname = "Annelie"; t68.lastname = "Hoff"; t68.teacherID = "aho"; t68.birthday = "501227"; teacherlist.Add(t68);
            teacherclass t69 = new teacherclass(); t69.name = "Anja Hoppe"; t69.firstname = "Anja"; t69.lastname = "Hoppe"; t69.teacherID = "ahp"; t69.birthday = "790222"; teacherlist.Add(t69);
            teacherclass t70 = new teacherclass(); t70.name = "Åsa Härstedt"; t70.firstname = "Åsa"; t70.lastname = "Härstedt"; t70.teacherID = "ahr"; t70.birthday = "670406"; teacherlist.Add(t70);
            teacherclass t71 = new teacherclass(); t71.name = "Andreas Hårrskog"; t71.firstname = "Andreas"; t71.lastname = "Hårrskog"; t71.teacherID = "ahs"; t71.birthday = "690928"; teacherlist.Add(t71);
            teacherclass t72 = new teacherclass(); t72.name = "Anna Holst"; t72.firstname = "Anna"; t72.lastname = "Holst"; t72.teacherID = "aht"; t72.birthday = "760614"; teacherlist.Add(t72);
            teacherclass t73 = new teacherclass(); t73.name = "Anders Hurtig"; t73.firstname = "Anders"; t73.lastname = "Hurtig"; t73.teacherID = "ahu"; t73.birthday = "711104"; teacherlist.Add(t73);
            teacherclass t74 = new teacherclass(); t74.name = "Amra Halilovic"; t74.firstname = "Amra"; t74.lastname = "Halilovic"; t74.teacherID = "ahv"; t74.birthday = "651218"; teacherlist.Add(t74);
            teacherclass t75 = new teacherclass(); t75.name = "Agneta Hybinette"; t75.firstname = "Agneta"; t75.lastname = "Hybinette"; t75.teacherID = "ahy"; t75.birthday = "640903"; teacherlist.Add(t75);
            teacherclass t76 = new teacherclass(); t76.name = "Amanda Jacobsen"; t76.firstname = "Amanda"; t76.lastname = "Jacobsen"; t76.teacherID = "ajc"; t76.birthday = "900115"; teacherlist.Add(t76);
            teacherclass t77 = new teacherclass(); t77.name = "Anna Jon-And"; t77.firstname = "Anna"; t77.lastname = "Jon-And"; t77.teacherID = "ajd"; t77.birthday = "760301"; teacherlist.Add(t77);
            teacherclass t78 = new teacherclass(); t78.name = "Anouk Jolin"; t78.firstname = "Anouk"; t78.lastname = "Jolin"; t78.teacherID = "ajl"; t78.birthday = "770803"; teacherlist.Add(t78);
            teacherclass t79 = new teacherclass(); t79.name = "Annie-Maj Johansson"; t79.firstname = "Annie-Maj"; t79.lastname = "Johansson"; t79.teacherID = "ajn"; t79.birthday = "600114"; teacherlist.Add(t79);
            teacherclass t80 = new teacherclass(); t80.name = "Annette Jönsson"; t80.firstname = "Annette"; t80.lastname = "Jönsson"; t80.teacherID = "ajo"; t80.birthday = "660508"; teacherlist.Add(t80);
            teacherclass t81 = new teacherclass(); t81.name = "Anna Jarstad"; t81.firstname = "Anna"; t81.lastname = "Jarstad"; t81.teacherID = "ajr"; t81.birthday = "660723"; teacherlist.Add(t81);
            teacherclass t82 = new teacherclass(); t82.name = "Andreas Isgren Karlsson"; t82.firstname = "Andreas"; t82.lastname = "Isgren Karlsson"; t82.teacherID = "aka"; t82.birthday = "821103"; teacherlist.Add(t82);
            teacherclass t83 = new teacherclass(); t83.name = "Anna Klerby"; t83.firstname = "Anna"; t83.lastname = "Klerby"; t83.teacherID = "akb"; t83.birthday = "770210"; teacherlist.Add(t83);
            teacherclass t84 = new teacherclass(); t84.name = "Åke Hestner"; t84.firstname = "Åke"; t84.lastname = "Hestner"; t84.teacherID = "ake"; t84.birthday = "620922"; teacherlist.Add(t84);
            teacherclass t85 = new teacherclass(); t85.name = "Anna Karin Fändrik"; t85.firstname = "Anna Karin"; t85.lastname = "Fändrik"; t85.teacherID = "akf"; t85.birthday = "610304"; teacherlist.Add(t85);
            teacherclass t86 = new teacherclass(); t86.name = "Ann-Sofie Källberg"; t86.firstname = "Ann-Sofie"; t86.lastname = "Källberg"; t86.teacherID = "akg"; t86.birthday = "601029"; teacherlist.Add(t86);
            teacherclass t87 = new teacherclass(); t87.name = "Ann-Kristin Hedlund"; t87.firstname = "Ann-Kristin"; t87.lastname = "Hedlund"; t87.teacherID = "akh"; t87.birthday = "651105"; teacherlist.Add(t87);
            teacherclass t88 = new teacherclass(); t88.name = "Anders Kjellsson"; t88.firstname = "Anders"; t88.lastname = "Kjellsson"; t88.teacherID = "akj"; t88.birthday = "700319"; teacherlist.Add(t88);
            teacherclass t89 = new teacherclass(); t89.name = "Anna-Karin Karlsson"; t89.firstname = "Anna-Karin"; t89.lastname = "Karlsson"; t89.teacherID = "akk"; t89.birthday = "721011"; teacherlist.Add(t89);
            teacherclass t90 = new teacherclass(); t90.name = "Anna-Karin Land"; t90.firstname = "Anna-Karin"; t90.lastname = "Land"; t90.teacherID = "akl"; t90.birthday = "710609"; teacherlist.Add(t90);
            teacherclass t91 = new teacherclass(); t91.name = "Ann Kammarbo"; t91.firstname = "Ann"; t91.lastname = "Kammarbo"; t91.teacherID = "akm"; t91.birthday = "681207"; teacherlist.Add(t91);
            teacherclass t92 = new teacherclass(); t92.name = "Akbar Khodabandehloo"; t92.firstname = "Akbar"; t92.lastname = "Khodabandehloo"; t92.teacherID = "ako"; t92.birthday = "520330"; teacherlist.Add(t92);
            teacherclass t93 = new teacherclass(); t93.name = "Anna Staff (Kruse)"; t93.firstname = "Anna"; t93.lastname = "Staff (Kruse)"; t93.teacherID = "akr"; t93.birthday = "820602"; teacherlist.Add(t93);
            teacherclass t94 = new teacherclass(); t94.name = "Anna Larsveden"; t94.firstname = "Anna"; t94.lastname = "Larsveden"; t94.teacherID = "ala"; t94.birthday = "700406"; teacherlist.Add(t94);
            teacherclass t95 = new teacherclass(); t95.name = "Anna Lindblom"; t95.firstname = "Anna"; t95.lastname = "Lindblom"; t95.teacherID = "alb"; t95.birthday = "790604"; teacherlist.Add(t95);
            teacherclass t96 = new teacherclass(); t96.name = "Anita Lidfors"; t96.firstname = "Anita"; t96.lastname = "Lidfors"; t96.teacherID = "ald"; t96.birthday = "560804"; teacherlist.Add(t96);
            teacherclass t97 = new teacherclass(); t97.name = "Annette Lenne"; t97.firstname = "Annette"; t97.lastname = "Lenne"; t97.teacherID = "ale"; t97.birthday = "650331"; teacherlist.Add(t97);
            teacherclass t98 = new teacherclass(); t98.name = "Anette Lindgren"; t98.firstname = "Anette"; t98.lastname = "Lindgren"; t98.teacherID = "alg"; t98.birthday = "690123"; teacherlist.Add(t98);
            teacherclass t99 = new teacherclass(); t99.name = "Anders Lindström"; t99.firstname = "Anders"; t99.lastname = "Lindström"; t99.teacherID = "ali"; t99.birthday = "490430"; teacherlist.Add(t99);
            teacherclass t100 = new teacherclass(); t100.name = "Alexander Karlsson"; t100.firstname = "Alexander"; t100.lastname = "Karlsson"; t100.teacherID = "alk"; t100.birthday = "840319"; teacherlist.Add(t100);
            teacherclass t101 = new teacherclass(); t101.name = "Andre Leblanc"; t101.firstname = "Andre"; t101.lastname = "Leblanc"; t101.teacherID = "all"; t101.birthday = "640914"; teacherlist.Add(t101);
            teacherclass t102 = new teacherclass(); t102.name = "Annacarin Linné"; t102.firstname = "Annacarin"; t102.lastname = "Linné"; t102.teacherID = "aln"; t102.birthday = "590828"; teacherlist.Add(t102);
            teacherclass t103 = new teacherclass(); t103.name = "Albina Pashkevich"; t103.firstname = "Albina"; t103.lastname = "Pashkevich"; t103.teacherID = "alp"; t103.birthday = "751016"; teacherlist.Add(t103);
            teacherclass t104 = new teacherclass(); t104.name = "Anders Lagerqvist"; t104.firstname = "Anders"; t104.lastname = "Lagerqvist"; t104.teacherID = "alq"; t104.birthday = "670415"; teacherlist.Add(t104);
            teacherclass t105 = new teacherclass(); t105.name = "Ales Svoboda"; t105.firstname = "Ales"; t105.lastname = "Svoboda"; t105.teacherID = "als"; t105.birthday = "480706"; teacherlist.Add(t105);
            teacherclass t106 = new teacherclass(); t106.name = "Alda Maria Lentina"; t106.firstname = "Alda Maria"; t106.lastname = "Lentina"; t106.teacherID = "alt"; t106.birthday = "700720"; teacherlist.Add(t106);
            teacherclass t107 = new teacherclass(); t107.name = "Anna Maria Dahlén"; t107.firstname = "Anna Maria"; t107.lastname = "Dahlén"; t107.teacherID = "amd"; t107.birthday = "650928"; teacherlist.Add(t107);
            teacherclass t108 = new teacherclass(); t108.name = "Anna-Maria Gylling"; t108.firstname = "Anna-Maria"; t108.lastname = "Gylling"; t108.teacherID = "amg"; t108.birthday = "620214"; teacherlist.Add(t108);
            teacherclass t109 = new teacherclass(); t109.name = "Abdikerim Mohamed Hasen"; t109.firstname = "Abdikerim"; t109.lastname = "Mohamed Hasen"; t109.teacherID = "amh"; t109.birthday = "790912"; teacherlist.Add(t109);
            teacherclass t110 = new teacherclass(); t110.name = "Amir Sattari"; t110.firstname = "Amir"; t110.lastname = "Sattari"; t110.teacherID = "ami"; t110.birthday = "800313"; teacherlist.Add(t110);
            teacherclass t111 = new teacherclass(); t111.name = "Ann-Marie Mohlin"; t111.firstname = "Ann-Marie"; t111.lastname = "Mohlin"; t111.teacherID = "amm"; t111.birthday = "570810"; teacherlist.Add(t111);
            teacherclass t112 = new teacherclass(); t112.name = "Anders Mattsson"; t112.firstname = "Anders"; t112.lastname = "Mattsson"; t112.teacherID = "amn"; t112.birthday = "490513"; teacherlist.Add(t112);
            teacherclass t113 = new teacherclass(); t113.name = "Anne-Marie Söderblom"; t113.firstname = "Anne-Marie"; t113.lastname = "Söderblom"; t113.teacherID = "ams"; t113.birthday = "660725"; teacherlist.Add(t113);
            teacherclass t114 = new teacherclass(); t114.name = "Åsa Mattsson"; t114.firstname = "Åsa"; t114.lastname = "Mattsson"; t114.teacherID = "amt"; t114.birthday = "621209"; teacherlist.Add(t114);
            teacherclass t115 = new teacherclass(); t115.name = "Anna Munters"; t115.firstname = "Anna"; t115.lastname = "Munters"; t115.teacherID = "amu"; t115.birthday = "650507"; teacherlist.Add(t115);
            teacherclass t116 = new teacherclass(); t116.name = "Anna-Marie Vanky"; t116.firstname = "Anna-Marie"; t116.lastname = "Vanky"; t116.teacherID = "amv"; t116.birthday = "671211"; teacherlist.Add(t116);
            teacherclass t117 = new teacherclass(); t117.name = "Anna Naylor"; t117.firstname = "Anna"; t117.lastname = "Naylor"; t117.teacherID = "ana"; t117.birthday = "660630"; teacherlist.Add(t117);
            teacherclass t118 = new teacherclass(); t118.name = "Annika Schmöker"; t118.firstname = "Annika"; t118.lastname = "Schmöker"; t118.teacherID = "anc"; t118.birthday = "830713"; teacherlist.Add(t118);
            teacherclass t119 = new teacherclass(); t119.name = "Anita Carlsson"; t119.firstname = "Anita"; t119.lastname = "Carlsson"; t119.teacherID = "anca"; t119.birthday = "630314"; teacherlist.Add(t119);
            teacherclass t120 = new teacherclass(); t120.name = "Andrea Lucarelli"; t120.firstname = "Andrea"; t120.lastname = "Lucarelli"; t120.teacherID = "ane"; t120.birthday = "820906"; teacherlist.Add(t120);
            teacherclass t121 = new teacherclass(); t121.name = "Anna Parkhouse"; t121.firstname = "Anna"; t121.lastname = "Parkhouse"; t121.teacherID = "anhe"; t121.birthday = "650226"; teacherlist.Add(t121);
            teacherclass t122 = new teacherclass(); t122.name = "Anna Hillström"; t122.firstname = "Anna"; t122.lastname = "Hillström"; t122.teacherID = "ani"; t122.birthday = "720806"; teacherlist.Add(t122);
            teacherclass t123 = new teacherclass(); t123.name = "Anna Åkerstedt (Björk)"; t123.firstname = "Anna"; t123.lastname = "Åkerstedt (Björk)"; t123.teacherID = "anj"; t123.birthday = "821112"; teacherlist.Add(t123);
            teacherclass t124 = new teacherclass(); t124.name = "Anna Laine"; t124.firstname = "Anna"; t124.lastname = "Laine"; t124.teacherID = "anl"; t124.birthday = "650611"; teacherlist.Add(t124);
            teacherclass t125 = new teacherclass(); t125.name = "Anna Sara Hammar"; t125.firstname = "Anna Sara"; t125.lastname = "Hammar"; t125.teacherID = "anm"; t125.birthday = "820312"; teacherlist.Add(t125);
            teacherclass t126 = new teacherclass(); t126.name = "Anna Hornström"; t126.firstname = "Anna"; t126.lastname = "Hornström"; t126.teacherID = "ann"; t126.birthday = "790320"; teacherlist.Add(t126);
            teacherclass t127 = new teacherclass(); t127.name = "Anders Nordahl"; t127.firstname = "Anders"; t127.lastname = "Nordahl"; t127.teacherID = "ano"; t127.birthday = "710125"; teacherlist.Add(t127);
            teacherclass t128 = new teacherclass(); t128.name = "André Lisspers"; t128.firstname = "André"; t128.lastname = "Lisspers"; t128.teacherID = "anp"; t128.birthday = "920904"; teacherlist.Add(t128);
            teacherclass t129 = new teacherclass(); t129.name = "Andrew Scott"; t129.firstname = "Andrew"; t129.lastname = "Scott"; t129.teacherID = "ansc"; t129.birthday = "560228"; teacherlist.Add(t129);
            teacherclass t130 = new teacherclass(); t130.name = "Anncarin Svanberg"; t130.firstname = "Anncarin"; t130.lastname = "Svanberg"; t130.teacherID = "anv"; t130.birthday = "560324"; teacherlist.Add(t130);
            teacherclass t131 = new teacherclass(); t131.name = "Anders Nygårds"; t131.firstname = "Anders"; t131.lastname = "Nygårds"; t131.teacherID = "any"; t131.birthday = "770707"; teacherlist.Add(t131);
            teacherclass t132 = new teacherclass(); t132.name = "Anette Överbring"; t132.firstname = "Anette"; t132.lastname = "Överbring"; t132.teacherID = "aov"; t132.birthday = "680326"; teacherlist.Add(t132);
            teacherclass t133 = new teacherclass(); t133.name = "Anders Persson"; t133.firstname = "Anders"; t133.lastname = "Persson"; t133.teacherID = "ape"; t133.birthday = "740309"; teacherlist.Add(t133);
            teacherclass t134 = new teacherclass(); t134.name = "Angela Poroli"; t134.firstname = "Angela"; t134.lastname = "Poroli"; t134.teacherID = "apl"; t134.birthday = "570526"; teacherlist.Add(t134);
            teacherclass t135 = new teacherclass(); t135.name = "Åke Persson"; t135.firstname = "Åke"; t135.lastname = "Persson"; t135.teacherID = "apo"; t135.birthday = "541106"; teacherlist.Add(t135);
            teacherclass t136 = new teacherclass(); t136.name = "Anita Purcell Sjölund"; t136.firstname = "Anita"; t136.lastname = "Purcell Sjölund"; t136.teacherID = "aps"; t136.birthday = "680313"; teacherlist.Add(t136);
            teacherclass t137 = new teacherclass(); t137.name = "Åsa Pettersson"; t137.firstname = "Åsa"; t137.lastname = "Pettersson"; t137.teacherID = "apt"; t137.birthday = "770401"; teacherlist.Add(t137);
            teacherclass t138 = new teacherclass(); t138.name = "Anders Ramsay"; t138.firstname = "Anders"; t138.lastname = "Ramsay"; t138.teacherID = "ara"; t138.birthday = "540816"; teacherlist.Add(t138);
            teacherclass t139 = new teacherclass(); t139.name = "Ann Rudman"; t139.firstname = "Ann"; t139.lastname = "Rudman"; t139.teacherID = "ard"; t139.birthday = "660422"; teacherlist.Add(t139);
            teacherclass t140 = new teacherclass(); t140.name = "Andreas Romeborn"; t140.firstname = "Andreas"; t140.lastname = "Romeborn"; t140.teacherID = "arm"; t140.birthday = "810304"; teacherlist.Add(t140);
            teacherclass t141 = new teacherclass(); t141.name = "Nejood Al-Rubaye"; t141.firstname = "Nejood"; t141.lastname = "Al-Rubaye"; t141.teacherID = "arn"; t141.birthday = "680625"; teacherlist.Add(t141);
            teacherclass t142 = new teacherclass(); t142.name = "Árni Sverrisson"; t142.firstname = "Árni"; t142.lastname = "Sverrisson"; t142.teacherID = "arsv"; t142.birthday = "530515"; teacherlist.Add(t142);
            teacherclass t143 = new teacherclass(); t143.name = "Agneta Rudberg"; t143.firstname = "Agneta"; t143.lastname = "Rudberg"; t143.teacherID = "aru"; t143.birthday = "540602"; teacherlist.Add(t143);
            teacherclass t144 = new teacherclass(); t144.name = "Alexis Rydell"; t144.firstname = "Alexis"; t144.lastname = "Rydell"; t144.teacherID = "ary"; t144.birthday = "820302"; teacherlist.Add(t144);
            teacherclass t145 = new teacherclass(); t145.name = "Agnes Godel (Sandin)"; t145.firstname = "Agnes"; t145.lastname = "Godel (Sandin)"; t145.teacherID = "asa"; t145.birthday = "640116"; teacherlist.Add(t145);
            teacherclass t146 = new teacherclass(); t146.name = "Anna Skogbergs"; t146.firstname = "Anna"; t146.lastname = "Skogbergs"; t146.teacherID = "asb"; t146.birthday = "790327"; teacherlist.Add(t146);
            teacherclass t147 = new teacherclass(); t147.name = "Andrea Schwachenwald"; t147.firstname = "Andrea"; t147.lastname = "Schwachenwald"; t147.teacherID = "asc"; t147.birthday = "601114"; teacherlist.Add(t147);
            teacherclass t148 = new teacherclass(); t148.name = "Anneli Södergren"; t148.firstname = "Anneli"; t148.lastname = "Södergren"; t148.teacherID = "asd"; t148.birthday = "601127"; teacherlist.Add(t148);
            teacherclass t149 = new teacherclass(); t149.name = "Anneli Strömsöe"; t149.firstname = "Anneli"; t149.lastname = "Strömsöe"; t149.teacherID = "ase"; t149.birthday = "691102"; teacherlist.Add(t149);
            teacherclass t150 = new teacherclass(); t150.name = "Anke Schmidt Felzmann"; t150.firstname = "Anke"; t150.lastname = "Schmidt Felzmann"; t150.teacherID = "asf"; t150.birthday = "770711"; teacherlist.Add(t150);
            teacherclass t151 = new teacherclass(); t151.name = "Anna Skoglund"; t151.firstname = "Anna"; t151.lastname = "Skoglund"; t151.teacherID = "asg"; t151.birthday = "580327"; teacherlist.Add(t151);
            teacherclass t152 = new teacherclass(); t152.name = "Anna-Sofia Hedberg"; t152.firstname = "Anna-Sofia"; t152.lastname = "Hedberg"; t152.teacherID = "ash"; t152.birthday = "770302"; teacherlist.Add(t152);
            teacherclass t153 = new teacherclass(); t153.name = "Asif Huq"; t153.firstname = "Asif"; t153.lastname = "Huq"; t153.teacherID = "ashu"; t153.birthday = "870312"; teacherlist.Add(t153);
            teacherclass t154 = new teacherclass(); t154.name = "Annika Selin Larsson"; t154.firstname = "Annika"; t154.lastname = "Selin Larsson"; t154.teacherID = "asi"; t154.birthday = "710317"; teacherlist.Add(t154);
            teacherclass t155 = new teacherclass(); t155.name = "Anna Sellner"; t155.firstname = "Anna"; t155.lastname = "Sellner"; t155.teacherID = "asl"; t155.birthday = "800826"; teacherlist.Add(t155);
            teacherclass t156 = new teacherclass(); t156.name = "Åsa Lang"; t156.firstname = "Åsa"; t156.lastname = "Lang"; t156.teacherID = "asla"; t156.birthday = "681204"; teacherlist.Add(t156);
            teacherclass t157 = new teacherclass(); t157.name = "Anna Ström"; t157.firstname = "Anna"; t157.lastname = "Ström"; t157.teacherID = "asm"; t157.birthday = "760925"; teacherlist.Add(t157);
            teacherclass t158 = new teacherclass(); t158.name = "Annelie Snis"; t158.firstname = "Annelie"; t158.lastname = "Snis"; t158.teacherID = "asn"; t158.birthday = "790112"; teacherlist.Add(t158);
            teacherclass t159 = new teacherclass(); t159.name = "Annica Skarpfors"; t159.firstname = "Annica"; t159.lastname = "Skarpfors"; t159.teacherID = "asp"; t159.birthday = "650215"; teacherlist.Add(t159);
            teacherclass t160 = new teacherclass(); t160.name = "Anette Sahlqvist"; t160.firstname = "Anette"; t160.lastname = "Sahlqvist"; t160.teacherID = "asq"; t160.birthday = "551015"; teacherlist.Add(t160);
            teacherclass t161 = new teacherclass(); t161.name = "Åsa Ström"; t161.firstname = "Åsa"; t161.lastname = "Ström"; t161.teacherID = "asr"; t161.birthday = "650207"; teacherlist.Add(t161);
            teacherclass t162 = new teacherclass(); t162.name = "Ann-Christin Stenman"; t162.firstname = "Ann-Christin"; t162.lastname = "Stenman"; t162.teacherID = "ast"; t162.birthday = "591002"; teacherlist.Add(t162);
            teacherclass t163 = new teacherclass(); t163.name = "Åsa Bergman Bruhn"; t163.firstname = "Åsa"; t163.lastname = "Bergman Bruhn"; t163.teacherID = "asu"; t163.birthday = "720320"; teacherlist.Add(t163);
            teacherclass t164 = new teacherclass(); t164.name = "Åsa Svensson"; t164.firstname = "Åsa"; t164.lastname = "Svensson"; t164.teacherID = "asv"; t164.birthday = "650407"; teacherlist.Add(t164);
            teacherclass t165 = new teacherclass(); t165.name = "Anna Swall"; t165.firstname = "Anna"; t165.lastname = "Swall"; t165.teacherID = "asw"; t165.birthday = "771219"; teacherlist.Add(t165);
            teacherclass t166 = new teacherclass(); t166.name = "Aranzazu Santos Muñoz"; t166.firstname = "Aranzazu"; t166.lastname = "Santos Muñoz"; t166.teacherID = "asz"; t166.birthday = "750427"; teacherlist.Add(t166);
            teacherclass t167 = new teacherclass(); t167.name = "Aili Tang"; t167.firstname = "Aili"; t167.lastname = "Tang"; t167.teacherID = "ata"; t167.birthday = "831230"; teacherlist.Add(t167);
            teacherclass t168 = new teacherclass(); t168.name = "Anna Teledahl"; t168.firstname = "Anna"; t168.lastname = "Teledahl"; t168.teacherID = "ate"; t168.birthday = "720315"; teacherlist.Add(t168);
            teacherclass t169 = new teacherclass(); t169.name = "Anita Thomas"; t169.firstname = "Anita"; t169.lastname = "Thomas"; t169.teacherID = "ath"; t169.birthday = "671130"; teacherlist.Add(t169);
            teacherclass t170 = new teacherclass(); t170.name = "Anette Timmerlid"; t170.firstname = "Anette"; t170.lastname = "Timmerlid"; t170.teacherID = "ati"; t170.birthday = "560526"; teacherlist.Add(t170);
            teacherclass t171 = new teacherclass(); t171.name = "Anders Törnqvist"; t171.firstname = "Anders"; t171.lastname = "Törnqvist"; t171.teacherID = "att"; t171.birthday = "680124"; teacherlist.Add(t171);
            teacherclass t172 = new teacherclass(); t172.name = "Anna Bäck Tunell"; t172.firstname = "Anna"; t172.lastname = "Bäck Tunell"; t172.teacherID = "atu"; t172.birthday = "680702"; teacherlist.Add(t172);
            teacherclass t173 = new teacherclass(); t173.name = "Anneli Wiberg"; t173.firstname = "Anneli"; t173.lastname = "Wiberg"; t173.teacherID = "awb"; t173.birthday = "710805"; teacherlist.Add(t173);
            teacherclass t174 = new teacherclass(); t174.name = "Åsa Wedin"; t174.firstname = "Åsa"; t174.lastname = "Wedin"; t174.teacherID = "awe"; t174.birthday = "550312"; teacherlist.Add(t174);
            teacherclass t175 = new teacherclass(); t175.name = "Anna Wibom"; t175.firstname = "Anna"; t175.lastname = "Wibom"; t175.teacherID = "awi"; t175.birthday = "691003"; teacherlist.Add(t175);
            teacherclass t176 = new teacherclass(); t176.name = "Anette Westerlund"; t176.firstname = "Anette"; t176.lastname = "Westerlund"; t176.teacherID = "awr"; t176.birthday = "551127"; teacherlist.Add(t176);
            teacherclass t177 = new teacherclass(); t177.name = "Per Axel Grigor"; t177.firstname = "Per Axel"; t177.lastname = "Grigor"; t177.teacherID = "axg"; t177.birthday = "751019"; teacherlist.Add(t177);
            teacherclass t178 = new teacherclass(); t178.name = "Barbro Lindman"; t178.firstname = "Barbro"; t178.lastname = "Lindman"; t178.teacherID = "babs"; t178.birthday = "681119"; teacherlist.Add(t178);
            teacherclass t179 = new teacherclass(); t179.name = "Björn Andersson"; t179.firstname = "Björn"; t179.lastname = "Andersson"; t179.teacherID = "bar"; t179.birthday = "790122"; teacherlist.Add(t179);
            teacherclass t180 = new teacherclass(); t180.name = "Barbara Bakker"; t180.firstname = "Barbara"; t180.lastname = "Bakker"; t180.teacherID = "bba"; t180.birthday = "660528"; teacherlist.Add(t180);
            teacherclass t181 = new teacherclass(); t181.name = "Barbro Carnehag"; t181.firstname = "Barbro"; t181.lastname = "Carnehag"; t181.teacherID = "bca"; t181.birthday = "500311"; teacherlist.Add(t181);
            teacherclass t182 = new teacherclass(); t182.name = "Bengt Erik Blomkvist"; t182.firstname = "Bengt Erik"; t182.lastname = "Blomkvist"; t182.teacherID = "beb"; t182.birthday = "600331"; teacherlist.Add(t182);
            teacherclass t183 = new teacherclass(); t183.name = "Bengt Ericsson"; t183.firstname = "Bengt"; t183.lastname = "Ericsson"; t183.teacherID = "bec"; t183.birthday = "570415"; teacherlist.Add(t183);
            teacherclass t184 = new teacherclass(); t184.name = "Bengt Eriksson"; t184.firstname = "Bengt"; t184.lastname = "Eriksson"; t184.teacherID = "bee"; t184.birthday = "501002"; teacherlist.Add(t184);
            teacherclass t185 = new teacherclass(); t185.name = "Bertil Valter Olsson"; t185.firstname = "Bertil Valter"; t185.lastname = "Olsson"; t185.teacherID = "beo"; t185.birthday = "500319"; teacherlist.Add(t185);
            teacherclass t186 = new teacherclass(); t186.name = "Björn Falkevall"; t186.firstname = "Björn"; t186.lastname = "Falkevall"; t186.teacherID = "bfl"; t186.birthday = "530728"; teacherlist.Add(t186);
            teacherclass t187 = new teacherclass(); t187.name = "Berit Gesar"; t187.firstname = "Berit"; t187.lastname = "Gesar"; t187.teacherID = "bge"; t187.birthday = "590513"; teacherlist.Add(t187);
            teacherclass t188 = new teacherclass(); t188.name = "Bo G Jansson"; t188.firstname = "Bo G"; t188.lastname = "Jansson"; t188.teacherID = "bgj"; t188.birthday = "490119"; teacherlist.Add(t188);
            teacherclass t189 = new teacherclass(); t189.name = "Billy Gray"; t189.firstname = "Billy"; t189.lastname = "Gray"; t189.teacherID = "bgr"; t189.birthday = "610306"; teacherlist.Add(t189);
            teacherclass t190 = new teacherclass(); t190.name = "Birgitta Hellmark-Lindgren"; t190.firstname = "Birgitta"; t190.lastname = "Hellmark-Lindgren"; t190.teacherID = "bha"; t190.birthday = "650928"; teacherlist.Add(t190);
            teacherclass t191 = new teacherclass(); t191.name = "Olof Björn Henriksson"; t191.firstname = "Olof Björn"; t191.lastname = "Henriksson"; t191.teacherID = "bhe"; t191.birthday = "441004"; teacherlist.Add(t191);
            teacherclass t192 = new teacherclass(); t192.name = "Bengt Haag"; t192.firstname = "Bengt"; t192.lastname = "Haag"; t192.teacherID = "bhg"; t192.birthday = "510730"; teacherlist.Add(t192);
            teacherclass t193 = new teacherclass(); t193.name = "Barbro Helgesson"; t193.firstname = "Barbro"; t193.lastname = "Helgesson"; t193.teacherID = "bhl"; t193.birthday = "530312"; teacherlist.Add(t193);
            teacherclass t194 = new teacherclass(); t194.name = "Bertil Hammarström"; t194.firstname = "Bertil"; t194.lastname = "Hammarström"; t194.teacherID = "bhm"; t194.birthday = "560131"; teacherlist.Add(t194);
            teacherclass t195 = new teacherclass(); t195.name = "Barbro Holmgren"; t195.firstname = "Barbro"; t195.lastname = "Holmgren"; t195.teacherID = "bho"; t195.birthday = "500430"; teacherlist.Add(t195);
            teacherclass t196 = new teacherclass(); t196.name = "Britta Hallpers"; t196.firstname = "Britta"; t196.lastname = "Hallpers"; t196.teacherID = "bhp"; t196.birthday = "490228"; teacherlist.Add(t196);
            teacherclass t197 = new teacherclass(); t197.name = "Bengt Höjer"; t197.firstname = "Bengt"; t197.lastname = "Höjer"; t197.teacherID = "bhr"; t197.birthday = "380407"; teacherlist.Add(t197);
            teacherclass t198 = new teacherclass(); t198.name = "Birgitta Svensson"; t198.firstname = "Birgitta"; t198.lastname = "Svensson"; t198.teacherID = "bibo"; t198.birthday = "671018"; teacherlist.Add(t198);
            teacherclass t199 = new teacherclass(); t199.name = "Björn Äng"; t199.firstname = "Björn"; t199.lastname = "Äng"; t199.teacherID = "bja"; t199.birthday = "670704"; teacherlist.Add(t199);
            teacherclass t200 = new teacherclass(); t200.name = "Björn Hammar"; t200.firstname = "Björn"; t200.lastname = "Hammar"; t200.teacherID = "bjh"; t200.birthday = "670517"; teacherlist.Add(t200);
            teacherclass t201 = new teacherclass(); t201.name = "Birgitta Jönsson"; t201.firstname = "Birgitta"; t201.lastname = "Jönsson"; t201.teacherID = "bjo"; t201.birthday = "620513"; teacherlist.Add(t201);
            teacherclass t202 = new teacherclass(); t202.name = "Britt Karlsson"; t202.firstname = "Britt"; t202.lastname = "Karlsson"; t202.teacherID = "bka"; t202.birthday = "561101"; teacherlist.Add(t202);
            teacherclass t203 = new teacherclass(); t203.name = "Berit Wallin Karlsson"; t203.firstname = "Berit"; t203.lastname = "Wallin Karlsson"; t203.teacherID = "bkr"; t203.birthday = "540420"; teacherlist.Add(t203);
            teacherclass t204 = new teacherclass(); t204.name = "Björn Larsson"; t204.firstname = "Björn"; t204.lastname = "Larsson"; t204.teacherID = "bla"; t204.birthday = "760308"; teacherlist.Add(t204);
            teacherclass t205 = new teacherclass(); t205.name = "Berit Lundgren"; t205.firstname = "Berit"; t205.lastname = "Lundgren"; t205.teacherID = "bld"; t205.birthday = "510706"; teacherlist.Add(t205);
            teacherclass t206 = new teacherclass(); t206.name = "Barbara Lees"; t206.firstname = "Barbara"; t206.lastname = "Lees"; t206.teacherID = "ble"; t206.birthday = "540219"; teacherlist.Add(t206);
            teacherclass t207 = new teacherclass(); t207.name = "Bengt Löfgren"; t207.firstname = "Bengt"; t207.lastname = "Löfgren"; t207.teacherID = "bln"; t207.birthday = "441214"; teacherlist.Add(t207);
            teacherclass t208 = new teacherclass(); t208.name = "Bo Larsson"; t208.firstname = "Bo"; t208.lastname = "Larsson"; t208.teacherID = "blr"; t208.birthday = "680726"; teacherlist.Add(t208);
            teacherclass t209 = new teacherclass(); t209.name = "Birgitta Larsson"; t209.firstname = "Birgitta"; t209.lastname = "Larsson"; t209.teacherID = "bls"; t209.birthday = "680426"; teacherlist.Add(t209);
            teacherclass t210 = new teacherclass(); t210.name = "Britt-Marie Löfgren"; t210.firstname = "Britt-Marie"; t210.lastname = "Löfgren"; t210.teacherID = "bml"; t210.birthday = "480216"; teacherlist.Add(t210);
            teacherclass t211 = new teacherclass(); t211.name = "Birgitta Nilsson"; t211.firstname = "Birgitta"; t211.lastname = "Nilsson"; t211.teacherID = "bni"; t211.birthday = "501109"; teacherlist.Add(t211);
            teacherclass t212 = new teacherclass(); t212.name = "Bodil Eriksson"; t212.firstname = "Bodil"; t212.lastname = "Eriksson"; t212.teacherID = "boe"; t212.birthday = "471113"; teacherlist.Add(t212);
            teacherclass t213 = new teacherclass(); t213.name = "Bengt-Olof Hed"; t213.firstname = "Bengt-Olof"; t213.lastname = "Hed"; t213.teacherID = "boh"; t213.birthday = "520207"; teacherlist.Add(t213);
            teacherclass t214 = new teacherclass(); t214.name = "Bengt Persson"; t214.firstname = "Bengt"; t214.lastname = "Persson"; t214.teacherID = "bpe"; t214.birthday = "461113"; teacherlist.Add(t214);
            teacherclass t215 = new teacherclass(); t215.name = "Bengt Persson"; t215.firstname = "Bengt"; t215.lastname = "Persson"; t215.teacherID = "bpn"; t215.birthday = "590716"; teacherlist.Add(t215);
            teacherclass t216 = new teacherclass(); t216.name = "Bengt Pontén"; t216.firstname = "Bengt"; t216.lastname = "Pontén"; t216.teacherID = "bpo"; t216.birthday = "551213"; teacherlist.Add(t216);
            teacherclass t217 = new teacherclass(); t217.name = "Britta Schaar"; t217.firstname = "Britta"; t217.lastname = "Schaar"; t217.teacherID = "bsc"; t217.birthday = "790225"; teacherlist.Add(t217);
            teacherclass t218 = new teacherclass(); t218.name = "Berk Sirman"; t218.firstname = "Berk"; t218.lastname = "Sirman"; t218.teacherID = "bsi"; t218.birthday = "801114"; teacherlist.Add(t218);
            teacherclass t219 = new teacherclass(); t219.name = "Boglárka Straszer"; t219.firstname = "Boglárka"; t219.lastname = "Straszer"; t219.teacherID = "bsr"; t219.birthday = "731031"; teacherlist.Add(t219);
            teacherclass t220 = new teacherclass(); t220.name = "Bo Sundgren"; t220.firstname = "Bo"; t220.lastname = "Sundgren"; t220.teacherID = "bsu"; t220.birthday = "460621"; teacherlist.Add(t220);
            teacherclass t221 = new teacherclass(); t221.name = "Bosse Thorén"; t221.firstname = "Bosse"; t221.lastname = "Thorén"; t221.teacherID = "bth"; t221.birthday = "520916"; teacherlist.Add(t221);
            teacherclass t222 = new teacherclass(); t222.name = "Brita Westberg"; t222.firstname = "Brita"; t222.lastname = "Westberg"; t222.teacherID = "bwe"; t222.birthday = "650313"; teacherlist.Add(t222);
            teacherclass t223 = new teacherclass(); t223.name = "Birgitta Wikström"; t223.firstname = "Birgitta"; t223.lastname = "Wikström"; t223.teacherID = "bwi"; t223.birthday = "491012"; teacherlist.Add(t223);
            teacherclass t224 = new teacherclass(); t224.name = "Bethanne Yoxsimer Paulsrud"; t224.firstname = "Bethanne"; t224.lastname = "Yoxsimer Paulsrud"; t224.teacherID = "byp"; t224.birthday = "670329"; teacherlist.Add(t224);
            teacherclass t225 = new teacherclass(); t225.name = "Bodil Zalesky"; t225.firstname = "Bodil"; t225.lastname = "Zalesky"; t225.teacherID = "bza"; t225.birthday = "540916"; teacherlist.Add(t225);
            teacherclass t226 = new teacherclass(); t226.name = "Cecilia Andersson"; t226.firstname = "Cecilia"; t226.lastname = "Andersson"; t226.teacherID = "cad"; t226.birthday = "610326"; teacherlist.Add(t226);
            teacherclass t227 = new teacherclass(); t227.name = "Carl-Axel Norman"; t227.firstname = "Carl-Axel"; t227.lastname = "Norman"; t227.teacherID = "can"; t227.birthday = "580610"; teacherlist.Add(t227);
            teacherclass t228 = new teacherclass(); t228.name = "Christopher Michel Bales"; t228.firstname = "Christopher Michel"; t228.lastname = "Bales"; t228.teacherID = "cba"; t228.birthday = "600915"; teacherlist.Add(t228);
            teacherclass t229 = new teacherclass(); t229.name = "Christina Birath"; t229.firstname = "Christina"; t229.lastname = "Birath"; t229.teacherID = "cbi"; t229.birthday = "860707"; teacherlist.Add(t229);
            teacherclass t230 = new teacherclass(); t230.name = "Carl Björknäs"; t230.firstname = "Carl"; t230.lastname = "Björknäs"; t230.teacherID = "cbj"; t230.birthday = "800325"; teacherlist.Add(t230);
            teacherclass t231 = new teacherclass(); t231.name = "Carina Bååth"; t231.firstname = "Carina"; t231.lastname = "Bååth"; t231.teacherID = "cbt"; t231.birthday = "571224"; teacherlist.Add(t231);
            teacherclass t232 = new teacherclass(); t232.name = "Christine Bozier"; t232.firstname = "Christine"; t232.lastname = "Bozier"; t232.teacherID = "cbz"; t232.birthday = "671020"; teacherlist.Add(t232);
            teacherclass t233 = new teacherclass(); t233.name = "Christine Eriksson"; t233.firstname = "Christine"; t233.lastname = "Eriksson"; t233.teacherID = "cce"; t233.birthday = "561210"; teacherlist.Add(t233);
            teacherclass t234 = new teacherclass(); t234.name = "Catia Cialani"; t234.firstname = "Catia"; t234.lastname = "Cialani"; t234.teacherID = "cci"; t234.birthday = "700313"; teacherlist.Add(t234);
            teacherclass t235 = new teacherclass(); t235.name = "Chatarina Edfeldt"; t235.firstname = "Chatarina"; t235.lastname = "Edfeldt"; t235.teacherID = "ced"; t235.birthday = "630228"; teacherlist.Add(t235);
            teacherclass t236 = new teacherclass(); t236.name = "Christiane Ederyd Engelbert"; t236.firstname = "Christiane"; t236.lastname = "Ederyd Engelbert"; t236.teacherID = "cee"; t236.birthday = "820815"; teacherlist.Add(t236);
            teacherclass t237 = new teacherclass(); t237.name = "Christina Engström"; t237.firstname = "Christina"; t237.lastname = "Engström"; t237.teacherID = "ceg"; t237.birthday = "700724"; teacherlist.Add(t237);
            teacherclass t238 = new teacherclass(); t238.name = "Catharina Enhörning"; t238.firstname = "Catharina"; t238.lastname = "Enhörning"; t238.teacherID = "cen"; t238.birthday = "690526"; teacherlist.Add(t238);
            teacherclass t239 = new teacherclass(); t239.name = "Christofer Eriksson"; t239.firstname = "Christofer"; t239.lastname = "Eriksson"; t239.teacherID = "cer"; t239.birthday = "810412"; teacherlist.Add(t239);
            teacherclass t240 = new teacherclass(); t240.name = "Christopher Fallqvist"; t240.firstname = "Christopher"; t240.lastname = "Fallqvist"; t240.teacherID = "cfa"; t240.birthday = "830924"; teacherlist.Add(t240);
            teacherclass t241 = new teacherclass(); t241.name = "Jonas Ullberg"; t241.firstname = "Jonas"; t241.lastname = "Ullberg"; t241.teacherID = "cfo"; t241.birthday = "710130"; teacherlist.Add(t241);
            teacherclass t242 = new teacherclass(); t242.name = "Christine Fredriksson"; t242.firstname = "Christine"; t242.lastname = "Fredriksson"; t242.teacherID = "cfr"; t242.birthday = "650813"; teacherlist.Add(t242);
            teacherclass t243 = new teacherclass(); t243.name = "Csilla Gál"; t243.firstname = "Csilla"; t243.lastname = "Gál"; t243.teacherID = "cga"; t243.birthday = "760725"; teacherlist.Add(t243);
            teacherclass t244 = new teacherclass(); t244.name = "Carl-Gustav Melen"; t244.firstname = "Carl-Gustav"; t244.lastname = "Melen"; t244.teacherID = "cgm"; t244.birthday = "470617"; teacherlist.Add(t244);
            teacherclass t245 = new teacherclass(); t245.name = "Carina Green"; t245.firstname = "Carina"; t245.lastname = "Green"; t245.teacherID = "cgr"; t245.birthday = "691109"; teacherlist.Add(t245);
            teacherclass t246 = new teacherclass(); t246.name = "Carina Gyll"; t246.firstname = "Carina"; t246.lastname = "Gyll"; t246.teacherID = "cgy"; t246.birthday = "750406"; teacherlist.Add(t246);
            teacherclass t247 = new teacherclass(); t247.name = "Carl Hansén"; t247.firstname = "Carl"; t247.lastname = "Hansén"; t247.teacherID = "cha"; t247.birthday = "530822"; teacherlist.Add(t247);
            teacherclass t248 = new teacherclass(); t248.name = "Claes Hellqvist"; t248.firstname = "Claes"; t248.lastname = "Hellqvist"; t248.teacherID = "che"; t248.birthday = "520724"; teacherlist.Add(t248);
            teacherclass t249 = new teacherclass(); t249.name = "Christina Haggren"; t249.firstname = "Christina"; t249.lastname = "Haggren"; t249.teacherID = "chg"; t249.birthday = "451010"; teacherlist.Add(t249);
            teacherclass t250 = new teacherclass(); t250.name = "Changli He"; t250.firstname = "Changli"; t250.lastname = "He"; t250.teacherID = "chh"; t250.birthday = "470222"; teacherlist.Add(t250);
            teacherclass t251 = new teacherclass(); t251.name = "Charlotte Hillervik"; t251.firstname = "Charlotte"; t251.lastname = "Hillervik"; t251.teacherID = "chi"; t251.birthday = "480331"; teacherlist.Add(t251);
            teacherclass t252 = new teacherclass(); t252.name = "Chatarina Höjer"; t252.firstname = "Chatarina"; t252.lastname = "Höjer"; t252.teacherID = "chj"; t252.birthday = "671229"; teacherlist.Add(t252);
            teacherclass t253 = new teacherclass(); t253.name = "Li Chong Hui(741278-2607)"; t253.firstname = "Li Chong"; t253.lastname = "Hui(741278-2607)"; t253.teacherID = "chl"; t253.birthday = "741218"; teacherlist.Add(t253);
            teacherclass t254 = new teacherclass(); t254.name = "Chong Hui Li"; t254.firstname = "Chong Hui"; t254.lastname = "Li"; t254.teacherID = "chl"; t254.birthday = "741278"; teacherlist.Add(t254);
            teacherclass t255 = new teacherclass(); t255.name = "Catharina Jakobsson Hillerström"; t255.firstname = "Catharina"; t255.lastname = "Jakobsson Hillerström"; t255.teacherID = "cja"; t255.birthday = "610902"; teacherlist.Add(t255);
            teacherclass t256 = new teacherclass(); t256.name = "Christian Kok Nielsen"; t256.firstname = "Christian"; t256.lastname = "Kok Nielsen"; t256.teacherID = "ckn"; t256.birthday = "870817"; teacherlist.Add(t256);
            teacherclass t257 = new teacherclass(); t257.name = "Christian Kullberg"; t257.firstname = "Christian"; t257.lastname = "Kullberg"; t257.teacherID = "cku"; t257.birthday = "570724"; teacherlist.Add(t257);
            teacherclass t258 = new teacherclass(); t258.name = "Charlotte Lindgren"; t258.firstname = "Charlotte"; t258.lastname = "Lindgren"; t258.teacherID = "cld"; t258.birthday = "691010"; teacherlist.Add(t258);
            teacherclass t259 = new teacherclass(); t259.name = "Charlie Lindgren"; t259.firstname = "Charlie"; t259.lastname = "Lindgren"; t259.teacherID = "clg"; t259.birthday = "860721"; teacherlist.Add(t259);
            teacherclass t260 = new teacherclass(); t260.name = "Christina Kullberg"; t260.firstname = "Christina"; t260.lastname = "Kullberg"; t260.teacherID = "clk"; t260.birthday = "730415"; teacherlist.Add(t260);
            teacherclass t261 = new teacherclass(); t261.name = "Carolina Leon Vegas"; t261.firstname = "Carolina"; t261.lastname = "Leon Vegas"; t261.teacherID = "clv"; t261.birthday = "740816"; teacherlist.Add(t261);
            teacherclass t262 = new teacherclass(); t262.name = "Cecilia Petersen-Mattsson"; t262.firstname = "Cecilia"; t262.lastname = "Petersen-Mattsson"; t262.teacherID = "cma"; t262.birthday = "700415"; teacherlist.Add(t262);
            teacherclass t263 = new teacherclass(); t263.name = "Carles Magriñá Badiella"; t263.firstname = "Carles"; t263.lastname = "Magriñá Badiella"; t263.teacherID = "cmb"; t263.birthday = "740215"; teacherlist.Add(t263);
            teacherclass t264 = new teacherclass(); t264.name = "Christer Malmgren"; t264.firstname = "Christer"; t264.lastname = "Malmgren"; t264.teacherID = "cml"; t264.birthday = "650722"; teacherlist.Add(t264);
            teacherclass t265 = new teacherclass(); t265.name = "Christina Mäcs Romander"; t265.firstname = "Christina"; t265.lastname = "Mäcs Romander"; t265.teacherID = "cmr"; t265.birthday = "410814"; teacherlist.Add(t265);
            teacherclass t266 = new teacherclass(); t266.name = "Caroline Maria Bastholm"; t266.firstname = "Caroline Maria"; t266.lastname = "Bastholm"; t266.teacherID = "cne"; t266.birthday = "810819"; teacherlist.Add(t266);
            teacherclass t267 = new teacherclass(); t267.name = "Catharina Höög Nyström"; t267.firstname = "Catharina"; t267.lastname = "Höög Nyström"; t267.teacherID = "cnh"; t267.birthday = "631116"; teacherlist.Add(t267);
            teacherclass t268 = new teacherclass(); t268.name = "Carin Nordström"; t268.firstname = "Carin"; t268.lastname = "Nordström"; t268.teacherID = "cnr"; t268.birthday = "711207"; teacherlist.Add(t268);
            teacherclass t269 = new teacherclass(); t269.name = "Christer Nyström"; t269.firstname = "Christer"; t269.lastname = "Nyström"; t269.teacherID = "cny"; t269.birthday = "550827"; teacherlist.Add(t269);
            teacherclass t270 = new teacherclass(); t270.name = "Carl-Olof Bernsand"; t270.firstname = "Carl-Olof"; t270.lastname = "Bernsand"; t270.teacherID = "cob"; t270.birthday = "790709"; teacherlist.Add(t270);
            teacherclass t271 = new teacherclass(); t271.name = "Carl Olsmats"; t271.firstname = "Carl"; t271.lastname = "Olsmats"; t271.teacherID = "cos"; t271.birthday = "610430"; teacherlist.Add(t271);
            teacherclass t272 = new teacherclass(); t272.name = "Christopher Patten"; t272.firstname = "Christopher"; t272.lastname = "Patten"; t272.teacherID = "cpa"; t272.birthday = "680709"; teacherlist.Add(t272);
            teacherclass t273 = new teacherclass(); t273.name = "Christina Pedersen"; t273.firstname = "Christina"; t273.lastname = "Pedersen"; t273.teacherID = "cpn"; t273.birthday = "611030"; teacherlist.Add(t273);
            teacherclass t274 = new teacherclass(); t274.name = "Celine Rocher Hahlin"; t274.firstname = "Celine"; t274.lastname = "Rocher Hahlin"; t274.teacherID = "crc"; t274.birthday = "740421"; teacherlist.Add(t274);
            teacherclass t275 = new teacherclass(); t275.name = "Christine Riedwyl Gottberg"; t275.firstname = "Christine"; t275.lastname = "Riedwyl Gottberg"; t275.teacherID = "crg"; t275.birthday = "590517"; teacherlist.Add(t275);
            teacherclass t276 = new teacherclass(); t276.name = "Christina Romlid"; t276.firstname = "Christina"; t276.lastname = "Romlid"; t276.teacherID = "cro"; t276.birthday = "600820"; teacherlist.Add(t276);
            teacherclass t277 = new teacherclass(); t277.name = "Camilla Söderberg"; t277.firstname = "Camilla"; t277.lastname = "Söderberg"; t277.teacherID = "csd"; t277.birthday = "710311"; teacherlist.Add(t277);
            teacherclass t278 = new teacherclass(); t278.name = "Catarina Stichini"; t278.firstname = "Catarina"; t278.lastname = "Stichini"; t278.teacherID = "csh"; t278.birthday = "720830"; teacherlist.Add(t278);
            teacherclass t279 = new teacherclass(); t279.name = "Christer Solefält"; t279.firstname = "Christer"; t279.lastname = "Solefält"; t279.teacherID = "cso"; t279.birthday = "710718"; teacherlist.Add(t279);
            teacherclass t280 = new teacherclass(); t280.name = "Christer Spolin"; t280.firstname = "Christer"; t280.lastname = "Spolin"; t280.teacherID = "csp"; t280.birthday = "531125"; teacherlist.Add(t280);
            teacherclass t281 = new teacherclass(); t281.name = "Cecilia Strandroth"; t281.firstname = "Cecilia"; t281.lastname = "Strandroth"; t281.teacherID = "csr"; t281.birthday = "741114"; teacherlist.Add(t281);
            teacherclass t282 = new teacherclass(); t282.name = "Christer Sundin"; t282.firstname = "Christer"; t282.lastname = "Sundin"; t282.teacherID = "csu"; t282.birthday = "660730"; teacherlist.Add(t282);
            teacherclass t283 = new teacherclass(); t283.name = "Carin Tärestam"; t283.firstname = "Carin"; t283.lastname = "Tärestam"; t283.teacherID = "cta"; t283.birthday = "550605"; teacherlist.Add(t283);
            teacherclass t284 = new teacherclass(); t284.name = "Charlotte Terner"; t284.firstname = "Charlotte"; t284.lastname = "Terner"; t284.teacherID = "ctr"; t284.birthday = "580818"; teacherlist.Add(t284);
            teacherclass t285 = new teacherclass(); t285.name = "Cajsa Weiberth"; t285.firstname = "Cajsa"; t285.lastname = "Weiberth"; t285.teacherID = "cwi"; t285.birthday = "840810"; teacherlist.Add(t285);
            teacherclass t286 = new teacherclass(); t286.name = "Cecilia Wijnbladh"; t286.firstname = "Cecilia"; t286.lastname = "Wijnbladh"; t286.teacherID = "cwj"; t286.birthday = "710808"; teacherlist.Add(t286);
            teacherclass t287 = new teacherclass(); t287.name = "Carina Wallerstein"; t287.firstname = "Carina"; t287.lastname = "Wallerstein"; t287.teacherID = "cwl"; t287.birthday = "640122"; teacherlist.Add(t287);
            teacherclass t288 = new teacherclass(); t288.name = "Camilla Widerlöv"; t288.firstname = "Camilla"; t288.lastname = "Widerlöv"; t288.teacherID = "cwr"; t288.birthday = "670323"; teacherlist.Add(t288);
            teacherclass t289 = new teacherclass(); t289.name = "Carin Vesterlund"; t289.firstname = "Carin"; t289.lastname = "Vesterlund"; t289.teacherID = "cvs"; t289.birthday = "830928"; teacherlist.Add(t289);
            teacherclass t290 = new teacherclass(); t290.name = "Christina Westerlund"; t290.firstname = "Christina"; t290.lastname = "Westerlund"; t290.teacherID = "cws"; t290.birthday = "551203"; teacherlist.Add(t290);
            teacherclass t291 = new teacherclass(); t291.name = "Christoffer Westlund"; t291.firstname = "Christoffer"; t291.lastname = "Westlund"; t291.teacherID = "cwt"; t291.birthday = "950728"; teacherlist.Add(t291);
            teacherclass t292 = new teacherclass(); t292.name = "Carmen Zamorano Llena"; t292.firstname = "Carmen"; t292.lastname = "Zamorano Llena"; t292.teacherID = "cza"; t292.birthday = "730802"; teacherlist.Add(t292);
            teacherclass t293 = new teacherclass(); t293.name = "Chuan-Zhong Li"; t293.firstname = "Chuan-Zhong"; t293.lastname = "Li"; t293.teacherID = "czl"; t293.birthday = "620721"; teacherlist.Add(t293);
            teacherclass t294 = new teacherclass(); t294.name = "Dick Åhman"; t294.firstname = "Dick"; t294.lastname = "Åhman"; t294.teacherID = "dah"; t294.birthday = "470724"; teacherlist.Add(t294);
            teacherclass t295 = new teacherclass(); t295.name = "David Flärdh"; t295.firstname = "David"; t295.lastname = "Flärdh"; t295.teacherID = "dan"; t295.birthday = "840428"; teacherlist.Add(t295);
            teacherclass t296 = new teacherclass(); t296.name = "Daniel Beckman"; t296.firstname = "Daniel"; t296.lastname = "Beckman"; t296.teacherID = "dbe"; t296.birthday = "811123"; teacherlist.Add(t296);
            teacherclass t297 = new teacherclass(); t297.name = "Daniel Brandt"; t297.firstname = "Daniel"; t297.lastname = "Brandt"; t297.teacherID = "dbr"; t297.birthday = "741115"; teacherlist.Add(t297);
            teacherclass t298 = new teacherclass(); t298.name = "Dennis Sjögren"; t298.firstname = "Dennis"; t298.lastname = "Sjögren"; t298.teacherID = "dempa"; t298.birthday = "750829"; teacherlist.Add(t298);
            teacherclass t299 = new teacherclass(); t299.name = "Daniel Fredriksson"; t299.firstname = "Daniel"; t299.lastname = "Fredriksson"; t299.teacherID = "dfr"; t299.birthday = "780214"; teacherlist.Add(t299);
            teacherclass t300 = new teacherclass(); t300.name = "Daniel Gräns"; t300.firstname = "Daniel"; t300.lastname = "Gräns"; t300.teacherID = "dga"; t300.birthday = "770221"; teacherlist.Add(t300);
            teacherclass t301 = new teacherclass(); t301.name = "David Gray"; t301.firstname = "David"; t301.lastname = "Gray"; t301.teacherID = "dgy"; t301.birthday = "800509"; teacherlist.Add(t301);
            teacherclass t302 = new teacherclass(); t302.name = "Daniel Hammarström"; t302.firstname = "Daniel"; t302.lastname = "Hammarström"; t302.teacherID = "dha"; t302.birthday = "840627"; teacherlist.Add(t302);
            teacherclass t303 = new teacherclass(); t303.name = "David Hammarbäck"; t303.firstname = "David"; t303.lastname = "Hammarbäck"; t303.teacherID = "dhb"; t303.birthday = "730125"; teacherlist.Add(t303);
            teacherclass t304 = new teacherclass(); t304.name = "Doris Hägglund"; t304.firstname = "Doris"; t304.lastname = "Hägglund"; t304.teacherID = "dhl"; t304.birthday = "491006"; teacherlist.Add(t304);
            teacherclass t305 = new teacherclass(); t305.name = "Diala Jomaa"; t305.firstname = "Diala"; t305.lastname = "Jomaa"; t305.teacherID = "djo"; t305.birthday = "780810"; teacherlist.Add(t305);
            teacherclass t306 = new teacherclass(); t306.name = "Kjetil Duvold"; t306.firstname = "Kjetil"; t306.lastname = "Duvold"; t306.teacherID = "dkj"; t306.birthday = "710410"; teacherlist.Add(t306);
            teacherclass t307 = new teacherclass(); t307.name = "Marie-Désirée Kroner"; t307.firstname = "Marie-Désirée"; t307.lastname = "Kroner"; t307.teacherID = "dkr"; t307.birthday = "780530"; teacherlist.Add(t307);
            teacherclass t308 = new teacherclass(); t308.name = "Daniel Löwenborg"; t308.firstname = "Daniel"; t308.lastname = "Löwenborg"; t308.teacherID = "dle"; t308.birthday = "750302"; teacherlist.Add(t308);
            teacherclass t309 = new teacherclass(); t309.name = "David Lifmark"; t309.firstname = "David"; t309.lastname = "Lifmark"; t309.teacherID = "dli"; t309.birthday = "710716"; teacherlist.Add(t309);
            teacherclass t310 = new teacherclass(); t310.name = "Daniel Nilsson"; t310.firstname = "Daniel"; t310.lastname = "Nilsson"; t310.teacherID = "dns"; t310.birthday = "691205"; teacherlist.Add(t310);
            teacherclass t311 = new teacherclass(); t311.name = "Daniel Olsson"; t311.firstname = "Daniel"; t311.lastname = "Olsson"; t311.teacherID = "dos"; t311.birthday = "790821"; teacherlist.Add(t311);
            teacherclass t312 = new teacherclass(); t312.name = "David Scott"; t312.firstname = "David"; t312.lastname = "Scott"; t312.teacherID = "dsc"; t312.birthday = "610986"; teacherlist.Add(t312);
            teacherclass t313 = new teacherclass(); t313.name = "Daniel Silander"; t313.firstname = "Daniel"; t313.lastname = "Silander"; t313.teacherID = "dsi"; t313.birthday = "721113"; teacherlist.Add(t313);
            teacherclass t314 = new teacherclass(); t314.name = "Daniel Sjögren"; t314.firstname = "Daniel"; t314.lastname = "Sjögren"; t314.teacherID = "dso"; t314.birthday = "750204"; teacherlist.Add(t314);
            teacherclass t315 = new teacherclass(); t315.name = "Daniel Sundberg"; t315.firstname = "Daniel"; t315.lastname = "Sundberg"; t315.teacherID = "dsu"; t315.birthday = "790724"; teacherlist.Add(t315);
            teacherclass t316 = new teacherclass(); t316.name = "Desiree Von Ahlefeld Nisser"; t316.firstname = "Desiree Von"; t316.lastname = "Ahlefeld Nisser"; t316.teacherID = "dva"; t316.birthday = "530717"; teacherlist.Add(t316);
            teacherclass t317 = new teacherclass(); t317.name = "David Wallefelt"; t317.firstname = "David"; t317.lastname = "Wallefelt"; t317.teacherID = "dwa"; t317.birthday = "740508"; teacherlist.Add(t317);
            teacherclass t318 = new teacherclass(); t318.name = "Dagmar Weidmann"; t318.firstname = "Dagmar"; t318.lastname = "Weidmann"; t318.teacherID = "dwd"; t318.birthday = "720109"; teacherlist.Add(t318);
            teacherclass t319 = new teacherclass(); t319.name = "Daniel Wikström"; t319.firstname = "Daniel"; t319.lastname = "Wikström"; t319.teacherID = "dwi"; t319.birthday = "761121"; teacherlist.Add(t319);
            teacherclass t320 = new teacherclass(); t320.name = "Diana Walve"; t320.firstname = "Diana"; t320.lastname = "Walve"; t320.teacherID = "dwl"; t320.birthday = "710502"; teacherlist.Add(t320);
            teacherclass t321 = new teacherclass(); t321.name = "Daniel Wallentin"; t321.firstname = "Daniel"; t321.lastname = "Wallentin"; t321.teacherID = "dwn"; t321.birthday = "810527"; teacherlist.Add(t321);
            teacherclass t322 = new teacherclass(); t322.name = "Dongyang Qu"; t322.firstname = "Dongyang"; t322.lastname = "Qu"; t322.teacherID = "dyq"; t322.birthday = "811024"; teacherlist.Add(t322);
            teacherclass t323 = new teacherclass(); t323.name = "Erika Andersson"; t323.firstname = "Erika"; t323.lastname = "Andersson"; t323.teacherID = "ead"; t323.birthday = "900220"; teacherlist.Add(t323);
            teacherclass t324 = new teacherclass(); t324.name = "Erik Falk"; t324.firstname = "Erik"; t324.lastname = "Falk"; t324.teacherID = "eaf"; t324.birthday = "730623"; teacherlist.Add(t324);
            teacherclass t325 = new teacherclass(); t325.name = "Elisabeth Åsenius Ahnberg"; t325.firstname = "Elisabeth"; t325.lastname = "Åsenius Ahnberg"; t325.teacherID = "eah"; t325.birthday = "790403"; teacherlist.Add(t325);
            teacherclass t326 = new teacherclass(); t326.name = "Elin Alsiok"; t326.firstname = "Elin"; t326.lastname = "Alsiok"; t326.teacherID = "eai"; t326.birthday = "850123"; teacherlist.Add(t326);
            teacherclass t327 = new teacherclass(); t327.name = "Elin Andersson"; t327.firstname = "Elin"; t327.lastname = "Andersson"; t327.teacherID = "ean"; t327.birthday = "770930"; teacherlist.Add(t327);
            teacherclass t328 = new teacherclass(); t328.name = "Emma Andersson"; t328.firstname = "Emma"; t328.lastname = "Andersson"; t328.teacherID = "ear"; t328.birthday = "920512"; teacherlist.Add(t328);
            teacherclass t329 = new teacherclass(); t329.name = "Eva Bäck"; t329.firstname = "Eva"; t329.lastname = "Bäck"; t329.teacherID = "eba"; t329.birthday = "491207"; teacherlist.Add(t329);
            teacherclass t330 = new teacherclass(); t330.name = "Erik Backman"; t330.firstname = "Erik"; t330.lastname = "Backman"; t330.teacherID = "ebk"; t330.birthday = "720411"; teacherlist.Add(t330);
            teacherclass t331 = new teacherclass(); t331.name = "Eva Berggård Nygren"; t331.firstname = "Eva"; t331.lastname = "Berggård Nygren"; t331.teacherID = "ebn"; t331.birthday = "540520"; teacherlist.Add(t331);
            teacherclass t332 = new teacherclass(); t332.name = "Ellen Borch"; t332.firstname = "Ellen"; t332.lastname = "Borch"; t332.teacherID = "ebr"; t332.birthday = "560112"; teacherlist.Add(t332);
            teacherclass t333 = new teacherclass(); t333.name = "Egle Berstiene"; t333.firstname = "Egle"; t333.lastname = "Berstiene"; t333.teacherID = "ebs"; t333.birthday = "730920"; teacherlist.Add(t333);
            teacherclass t334 = new teacherclass(); t334.name = "Erik Brunnert Walfridsson"; t334.firstname = "Erik"; t334.lastname = "Brunnert Walfridsson"; t334.teacherID = "ebw"; t334.birthday = "830730"; teacherlist.Add(t334);
            teacherclass t335 = new teacherclass(); t335.name = "Eva Elfvin Eriksson"; t335.firstname = "Eva"; t335.lastname = "Elfvin Eriksson"; t335.teacherID = "eee"; t335.birthday = "650211"; teacherlist.Add(t335);
            teacherclass t336 = new teacherclass(); t336.name = "Eva-Lena Erixon"; t336.firstname = "Eva-Lena"; t336.lastname = "Erixon"; t336.teacherID = "eer"; t336.birthday = "680104"; teacherlist.Add(t336);
            teacherclass t337 = new teacherclass(); t337.name = "Elin Eriksson"; t337.firstname = "Elin"; t337.lastname = "Eriksson"; t337.teacherID = "ees"; t337.birthday = "800215"; teacherlist.Add(t337);
            teacherclass t338 = new teacherclass(); t338.name = "Emil Gustafsson"; t338.firstname = "Emil"; t338.lastname = "Gustafsson"; t338.teacherID = "egu"; t338.birthday = "830502"; teacherlist.Add(t338);
            teacherclass t339 = new teacherclass(); t339.name = "Emelie Hebert"; t339.firstname = "Emelie"; t339.lastname = "Hebert"; t339.teacherID = "ehb"; t339.birthday = "800320"; teacherlist.Add(t339);
            teacherclass t340 = new teacherclass(); t340.name = "Erik Hedlund"; t340.firstname = "Erik"; t340.lastname = "Hedlund"; t340.teacherID = "ehe"; t340.birthday = "531125"; teacherlist.Add(t340);
            teacherclass t341 = new teacherclass(); t341.name = "Eva Hagström"; t341.firstname = "Eva"; t341.lastname = "Hagström"; t341.teacherID = "ehg"; t341.birthday = "630606"; teacherlist.Add(t341);
            teacherclass t342 = new teacherclass(); t342.name = "Emilia Henriksson"; t342.firstname = "Emilia"; t342.lastname = "Henriksson"; t342.teacherID = "ehi"; t342.birthday = "840923"; teacherlist.Add(t342);
            teacherclass t343 = new teacherclass(); t343.name = "Elin Hedman Eriksson"; t343.firstname = "Elin"; t343.lastname = "Hedman Eriksson"; t343.teacherID = "ehk"; t343.birthday = "911112"; teacherlist.Add(t343);
            teacherclass t344 = new teacherclass(); t344.name = "Emelie Hägglund"; t344.firstname = "Emelie"; t344.lastname = "Hägglund"; t344.teacherID = "ehl"; t344.birthday = "890729"; teacherlist.Add(t344);
            teacherclass t345 = new teacherclass(); t345.name = "Elin Holmsten"; t345.firstname = "Elin"; t345.lastname = "Holmsten"; t345.teacherID = "ehm"; t345.birthday = "720127"; teacherlist.Add(t345);
            teacherclass t346 = new teacherclass(); t346.name = "Eva Hintz-Nilsson"; t346.firstname = "Eva"; t346.lastname = "Hintz-Nilsson"; t346.teacherID = "ehn"; t346.birthday = "710806"; teacherlist.Add(t346);
            teacherclass t347 = new teacherclass(); t347.name = "Eva Hämberg"; t347.firstname = "Eva"; t347.lastname = "Hämberg"; t347.teacherID = "ehr"; t347.birthday = "581022"; teacherlist.Add(t347);
            teacherclass t348 = new teacherclass(); t348.name = "Eva Hultin"; t348.firstname = "Eva"; t348.lastname = "Hultin"; t348.teacherID = "ehu"; t348.birthday = "640710"; teacherlist.Add(t348);
            teacherclass t349 = new teacherclass(); t349.name = "Emma Hawke"; t349.firstname = "Emma"; t349.lastname = "Hawke"; t349.teacherID = "ehw"; t349.birthday = "751112"; teacherlist.Add(t349);
            teacherclass t350 = new teacherclass(); t350.name = "Erik Hysing"; t350.firstname = "Erik"; t350.lastname = "Hysing"; t350.teacherID = "ehy"; t350.birthday = "771007"; teacherlist.Add(t350);
            teacherclass t351 = new teacherclass(); t351.name = "Erika Iljero"; t351.firstname = "Erika"; t351.lastname = "Iljero"; t351.teacherID = "eil"; t351.birthday = "800807"; teacherlist.Add(t351);
            teacherclass t352 = new teacherclass(); t352.name = "Emil Johansson"; t352.firstname = "Emil"; t352.lastname = "Johansson"; t352.teacherID = "eja"; t352.birthday = "871215"; teacherlist.Add(t352);
            teacherclass t353 = new teacherclass(); t353.name = "Eva Jonasson"; t353.firstname = "Eva"; t353.lastname = "Jonasson"; t353.teacherID = "ejn"; t353.birthday = "601231"; teacherlist.Add(t353);
            teacherclass t354 = new teacherclass(); t354.name = "Elisabeth Jobs"; t354.firstname = "Elisabeth"; t354.lastname = "Jobs"; t354.teacherID = "ejo"; t354.birthday = "681127"; teacherlist.Add(t354);
            teacherclass t355 = new teacherclass(); t355.name = "Eva Karlsson"; t355.firstname = "Eva"; t355.lastname = "Karlsson"; t355.teacherID = "eka"; t355.birthday = "531014"; teacherlist.Add(t355);
            teacherclass t356 = new teacherclass(); t356.name = "Edmund Knutas"; t356.firstname = "Edmund"; t356.lastname = "Knutas"; t356.teacherID = "ekn"; t356.birthday = "520924"; teacherlist.Add(t356);
            teacherclass t357 = new teacherclass(); t357.name = "Emma Knutsson"; t357.firstname = "Emma"; t357.lastname = "Knutsson"; t357.teacherID = "eku"; t357.birthday = "830721"; teacherlist.Add(t357);
            teacherclass t358 = new teacherclass(); t358.name = "Erik Lundberg"; t358.firstname = "Erik"; t358.lastname = "Lundberg"; t358.teacherID = "elb"; t358.birthday = "800523"; teacherlist.Add(t358);
            teacherclass t359 = new teacherclass(); t359.name = "Erik Leander"; t359.firstname = "Erik"; t359.lastname = "Leander"; t359.teacherID = "eld"; t359.birthday = "810914"; teacherlist.Add(t359);
            teacherclass t360 = new teacherclass(); t360.name = "Eva-Lena Embretsen"; t360.firstname = "Eva-Lena"; t360.lastname = "Embretsen"; t360.teacherID = "ele"; t360.birthday = "590307"; teacherlist.Add(t360);
            teacherclass t361 = new teacherclass(); t361.name = "Ewa-Lena Fränkel"; t361.firstname = "Ewa-Lena"; t361.lastname = "Fränkel"; t361.teacherID = "elf"; t361.birthday = "530612"; teacherlist.Add(t361);
            teacherclass t362 = new teacherclass(); t362.name = "Elisabeth Eneflo Lindgren"; t362.firstname = "Elisabeth"; t362.lastname = "Eneflo Lindgren"; t362.teacherID = "elg"; t362.birthday = "561030"; teacherlist.Add(t362);
            teacherclass t363 = new teacherclass(); t363.name = "Elisabeth Lindberg"; t363.firstname = "Elisabeth"; t363.lastname = "Lindberg"; t363.teacherID = "eln"; t363.birthday = "660921"; teacherlist.Add(t363);
            teacherclass t364 = new teacherclass(); t364.name = "Eva Löfstrand"; t364.firstname = "Eva"; t364.lastname = "Löfstrand"; t364.teacherID = "elo"; t364.birthday = "551122"; teacherlist.Add(t364);
            teacherclass t365 = new teacherclass(); t365.name = "Elisabet Björklund"; t365.firstname = "Elisabet"; t365.lastname = "Björklund"; t365.teacherID = "elu"; t365.birthday = "681019"; teacherlist.Add(t365);
            teacherclass t366 = new teacherclass(); t366.name = "Eva Maritz"; t366.firstname = "Eva"; t366.lastname = "Maritz"; t366.teacherID = "ema"; t366.birthday = "581208"; teacherlist.Add(t366);
            teacherclass t367 = new teacherclass(); t367.name = "Emin Halilovic"; t367.firstname = "Emin"; t367.lastname = "Halilovic"; t367.teacherID = "emin"; t367.birthday = "530706"; teacherlist.Add(t367);
            teacherclass t368 = new teacherclass(); t368.name = "Elin Storman (Fd Sundin)"; t368.firstname = "Elin"; t368.lastname = "Storman (Fd Sundin)"; t368.teacherID = "ems"; t368.birthday = "900412"; teacherlist.Add(t368);
            teacherclass t369 = new teacherclass(); t369.name = "Napolion Edgar Asiimwe"; t369.firstname = "Napolion Edgar"; t369.lastname = "Asiimwe"; t369.teacherID = "ena"; t369.birthday = "840414"; teacherlist.Add(t369);
            teacherclass t370 = new teacherclass(); t370.name = "Emelie Westergren"; t370.firstname = "Emelie"; t370.lastname = "Westergren"; t370.teacherID = "enb"; t370.birthday = "810625"; teacherlist.Add(t370);
            teacherclass t371 = new teacherclass(); t371.name = "Eva Nordgren"; t371.firstname = "Eva"; t371.lastname = "Nordgren"; t371.teacherID = "end"; t371.birthday = "630521"; teacherlist.Add(t371);
            teacherclass t372 = new teacherclass(); t372.name = "Elisabeth Nerpin"; t372.firstname = "Elisabeth"; t372.lastname = "Nerpin"; t372.teacherID = "ene"; t372.birthday = "620529"; teacherlist.Add(t372);
            teacherclass t373 = new teacherclass(); t373.name = "Eva Söderlund"; t373.firstname = "Eva"; t373.lastname = "Söderlund"; t373.teacherID = "eni"; t373.birthday = "620709"; teacherlist.Add(t373);
            teacherclass t374 = new teacherclass(); t374.name = "Elisabet Odevik"; t374.firstname = "Elisabet"; t374.lastname = "Odevik"; t374.teacherID = "eod"; t374.birthday = "580419"; teacherlist.Add(t374);
            teacherclass t375 = new teacherclass(); t375.name = "Eva Österlund Efraimsson"; t375.firstname = "Eva"; t375.lastname = "Österlund Efraimsson"; t375.teacherID = "eoe"; t375.birthday = "610224"; teacherlist.Add(t375);
            teacherclass t376 = new teacherclass(); t376.name = "Emmanouil Psimopoulos"; t376.firstname = "Emmanouil"; t376.lastname = "Psimopoulos"; t376.teacherID = "eps"; t376.birthday = "781126"; teacherlist.Add(t376);
            teacherclass t377 = new teacherclass(); t377.name = "Eva Palm-Uhlin"; t377.firstname = "Eva"; t377.lastname = "Palm-Uhlin"; t377.teacherID = "epu"; t377.birthday = "721224"; teacherlist.Add(t377);
            teacherclass t378 = new teacherclass(); t378.name = "Eva Randell"; t378.firstname = "Eva"; t378.lastname = "Randell"; t378.teacherID = "era"; t378.birthday = "600912"; teacherlist.Add(t378);
            teacherclass t379 = new teacherclass(); t379.name = "Erik Arén"; t379.firstname = "Erik"; t379.lastname = "Arén"; t379.teacherID = "err"; t379.birthday = "880224"; teacherlist.Add(t379);
            teacherclass t380 = new teacherclass(); t380.name = "Eva Stattin"; t380.firstname = "Eva"; t380.lastname = "Stattin"; t380.teacherID = "esa"; t380.birthday = "580531"; teacherlist.Add(t380);
            teacherclass t381 = new teacherclass(); t381.name = "Erica Schytt"; t381.firstname = "Erica"; t381.lastname = "Schytt"; t381.teacherID = "esc"; t381.birthday = "590813"; teacherlist.Add(t381);
            teacherclass t382 = new teacherclass(); t382.name = "Emma Sandberg"; t382.firstname = "Emma"; t382.lastname = "Sandberg"; t382.teacherID = "esd"; t382.birthday = "761212"; teacherlist.Add(t382);
            teacherclass t383 = new teacherclass(); t383.name = "Ellinor Säfström"; t383.firstname = "Ellinor"; t383.lastname = "Säfström"; t383.teacherID = "esf"; t383.birthday = "790406"; teacherlist.Add(t383);
            teacherclass t384 = new teacherclass(); t384.name = "Emma Skoog"; t384.firstname = "Emma"; t384.lastname = "Skoog"; t384.teacherID = "esk"; t384.birthday = "860817"; teacherlist.Add(t384);
            teacherclass t385 = new teacherclass(); t385.name = "Elizaveta Soldatova"; t385.firstname = "Elizaveta"; t385.lastname = "Soldatova"; t385.teacherID = "esl"; t385.birthday = "761227"; teacherlist.Add(t385);
            teacherclass t386 = new teacherclass(); t386.name = "Elisabeth Svensdotter"; t386.firstname = "Elisabeth"; t386.lastname = "Svensdotter"; t386.teacherID = "esn"; t386.birthday = "570817"; teacherlist.Add(t386);
            teacherclass t387 = new teacherclass(); t387.name = "Eilert Söräng"; t387.firstname = "Eilert"; t387.lastname = "Söräng"; t387.teacherID = "eso"; t387.birthday = "510901"; teacherlist.Add(t387);
            teacherclass t388 = new teacherclass(); t388.name = "Elisabeth Wallin"; t388.firstname = "Elisabeth"; t388.lastname = "Wallin"; t388.teacherID = "ewa"; t388.birthday = "701215"; teacherlist.Add(t388);
            teacherclass t389 = new teacherclass(); t389.name = "Eva Österlind"; t389.firstname = "Eva"; t389.lastname = "Österlind"; t389.teacherID = "evao"; t389.birthday = "560918"; teacherlist.Add(t389);
            teacherclass t390 = new teacherclass(); t390.name = "Eva Taflin"; t390.firstname = "Eva"; t390.lastname = "Taflin"; t390.teacherID = "evat"; t390.birthday = "510825"; teacherlist.Add(t390);
            teacherclass t391 = new teacherclass(); t391.name = "Ewa Wäckelgård"; t391.firstname = "Ewa"; t391.lastname = "Wäckelgård"; t391.teacherID = "ewc"; t391.birthday = "570706"; teacherlist.Add(t391);
            teacherclass t392 = new teacherclass(); t392.name = "Erik Westholm"; t392.firstname = "Erik"; t392.lastname = "Westholm"; t392.teacherID = "ewe"; t392.birthday = "510602"; teacherlist.Add(t392);
            teacherclass t393 = new teacherclass(); t393.name = "Elin Wester"; t393.firstname = "Elin"; t393.lastname = "Wester"; t393.teacherID = "ews"; t393.birthday = "760818"; teacherlist.Add(t393);
            teacherclass t394 = new teacherclass(); t394.name = "Ergin Yucel"; t394.firstname = "Ergin"; t394.lastname = "Yucel"; t394.teacherID = "eyu"; t394.birthday = "760522"; teacherlist.Add(t394);
            teacherclass t395 = new teacherclass(); t395.name = "Fadi Abou Dib"; t395.firstname = "Fadi"; t395.lastname = "Abou Dib"; t395.teacherID = "fad"; t395.birthday = "850980"; teacherlist.Add(t395);
            teacherclass t396 = new teacherclass(); t396.name = "Fredrik Bökman"; t396.firstname = "Fredrik"; t396.lastname = "Bökman"; t396.teacherID = "fbo"; t396.birthday = "701031"; teacherlist.Add(t396);
            teacherclass t397 = new teacherclass(); t397.name = "Farhana Borg"; t397.firstname = "Farhana"; t397.lastname = "Borg"; t397.teacherID = "fbr"; t397.birthday = "670613"; teacherlist.Add(t397);
            teacherclass t398 = new teacherclass(); t398.name = "Fernando Padilla Camacho"; t398.firstname = "Fernando"; t398.lastname = "Padilla Camacho"; t398.teacherID = "fcp"; t398.birthday = "790420"; teacherlist.Add(t398);
            teacherclass t399 = new teacherclass(); t399.name = "Fredrik Ericsson"; t399.firstname = "Fredrik"; t399.lastname = "Ericsson"; t399.teacherID = "fei"; t399.birthday = "780918"; teacherlist.Add(t399);
            teacherclass t400 = new teacherclass(); t400.name = "Frank Fiedler"; t400.firstname = "Frank"; t400.lastname = "Fiedler"; t400.teacherID = "ffi"; t400.birthday = "710317"; teacherlist.Add(t400);
            teacherclass t401 = new teacherclass(); t401.name = "Frej Hallgren"; t401.firstname = "Frej"; t401.lastname = "Hallgren"; t401.teacherID = "fhl"; t401.birthday = "780210"; teacherlist.Add(t401);
            teacherclass t402 = new teacherclass(); t402.name = "Fredrik Hartwig"; t402.firstname = "Fredrik"; t402.lastname = "Hartwig"; t402.teacherID = "fhr"; t402.birthday = "761229"; teacherlist.Add(t402);
            teacherclass t403 = new teacherclass(); t403.name = "Frank Hayford"; t403.firstname = "Frank"; t403.lastname = "Hayford"; t403.teacherID = "fhy"; t403.birthday = "840402"; teacherlist.Add(t403);
            teacherclass t404 = new teacherclass(); t404.name = "Fredrik Karlsson"; t404.firstname = "Fredrik"; t404.lastname = "Karlsson"; t404.teacherID = "fka"; t404.birthday = "770404"; teacherlist.Add(t404);
            teacherclass t405 = new teacherclass(); t405.name = "Fredrik Larses"; t405.firstname = "Fredrik"; t405.lastname = "Larses"; t405.teacherID = "fla"; t405.birthday = "750120"; teacherlist.Add(t405);
            teacherclass t406 = new teacherclass(); t406.name = "Fredrik Lindberg"; t406.firstname = "Fredrik"; t406.lastname = "Lindberg"; t406.teacherID = "fld"; t406.birthday = "670128"; teacherlist.Add(t406);
            teacherclass t407 = new teacherclass(); t407.name = "Frans Lettenström"; t407.firstname = "Frans"; t407.lastname = "Lettenström"; t407.teacherID = "fle"; t407.birthday = "550317"; teacherlist.Add(t407);
            teacherclass t408 = new teacherclass(); t408.name = "Filippa Bergqvist"; t408.firstname = "Filippa"; t408.lastname = "Bergqvist"; t408.teacherID = "fli"; t408.birthday = "750316"; teacherlist.Add(t408);
            teacherclass t409 = new teacherclass(); t409.name = "Fredrik Land"; t409.firstname = "Fredrik"; t409.lastname = "Land"; t409.teacherID = "fln"; t409.birthday = "700515"; teacherlist.Add(t409);
            teacherclass t410 = new teacherclass(); t410.name = "Ming Fan"; t410.firstname = "Ming"; t410.lastname = "Fan"; t410.teacherID = "fmi"; t410.birthday = "560423"; teacherlist.Add(t410);
            teacherclass t411 = new teacherclass(); t411.name = "Fredrik Nilsson"; t411.firstname = "Fredrik"; t411.lastname = "Nilsson"; t411.teacherID = "fni"; t411.birthday = "720726"; teacherlist.Add(t411);
            teacherclass t412 = new teacherclass(); t412.name = "Fatumo Osman"; t412.firstname = "Fatumo"; t412.lastname = "Osman"; t412.teacherID = "fos"; t412.birthday = "731228"; teacherlist.Add(t412);
            teacherclass t413 = new teacherclass(); t413.name = "Fredrik Palm"; t413.firstname = "Fredrik"; t413.lastname = "Palm"; t413.teacherID = "fpa"; t413.birthday = "730226"; teacherlist.Add(t413);
            teacherclass t414 = new teacherclass(); t414.name = "Fredrik Remes"; t414.firstname = "Fredrik"; t414.lastname = "Remes"; t414.teacherID = "fre"; t414.birthday = "860322"; teacherlist.Add(t414);
            teacherclass t415 = new teacherclass(); t415.name = "Frida Splendido"; t415.firstname = "Frida"; t415.lastname = "Splendido"; t415.teacherID = "fsp"; t415.birthday = "820925"; teacherlist.Add(t415);
            teacherclass t416 = new teacherclass(); t416.name = "Frank Sundh"; t416.firstname = "Frank"; t416.lastname = "Sundh"; t416.teacherID = "fsu"; t416.birthday = "510811"; teacherlist.Add(t416);
            teacherclass t417 = new teacherclass(); t417.name = "Yang Fan Wallentin"; t417.firstname = "Yang Fan"; t417.lastname = "Wallentin"; t417.teacherID = "fwa"; t417.birthday = "621018"; teacherlist.Add(t417);
            teacherclass t418 = new teacherclass(); t418.name = "Fredrik Von Euler"; t418.firstname = "Fredrik Von"; t418.lastname = "Euler"; t418.teacherID = "fve"; t418.birthday = "571023"; teacherlist.Add(t418);
            teacherclass t419 = new teacherclass(); t419.name = "Gunnel Balaile"; t419.firstname = "Gunnel"; t419.lastname = "Balaile"; t419.teacherID = "gba"; t419.birthday = "441107"; teacherlist.Add(t419);
            teacherclass t420 = new teacherclass(); t420.name = "Gustav Boklund"; t420.firstname = "Gustav"; t420.lastname = "Boklund"; t420.teacherID = "gbk"; t420.birthday = "500916"; teacherlist.Add(t420);
            teacherclass t421 = new teacherclass(); t421.name = "Gabor Bora"; t421.firstname = "Gabor"; t421.lastname = "Bora"; t421.teacherID = "gbo"; t421.birthday = "560326"; teacherlist.Add(t421);
            teacherclass t422 = new teacherclass(); t422.name = "Gudrun Brundin"; t422.firstname = "Gudrun"; t422.lastname = "Brundin"; t422.teacherID = "gbu"; t422.birthday = "651023"; teacherlist.Add(t422);
            teacherclass t423 = new teacherclass(); t423.name = "Gunilla Carstensen"; t423.firstname = "Gunilla"; t423.lastname = "Carstensen"; t423.teacherID = "gca"; t423.birthday = "640408"; teacherlist.Add(t423);
            teacherclass t424 = new teacherclass(); t424.name = "Gianluca Colella"; t424.firstname = "Gianluca"; t424.lastname = "Colella"; t424.teacherID = "gco"; t424.birthday = "810113"; teacherlist.Add(t424);
            teacherclass t425 = new teacherclass(); t425.name = "Gudrun Elvhage"; t425.firstname = "Gudrun"; t425.lastname = "Elvhage"; t425.teacherID = "gel"; t425.birthday = "530815"; teacherlist.Add(t425);
            teacherclass t426 = new teacherclass(); t426.name = "Göran Engberg"; t426.firstname = "Göran"; t426.lastname = "Engberg"; t426.teacherID = "gen"; t426.birthday = "461223"; teacherlist.Add(t426);
            teacherclass t427 = new teacherclass(); t427.name = "Göran Enoksson"; t427.firstname = "Göran"; t427.lastname = "Enoksson"; t427.teacherID = "geo"; t427.birthday = "501221"; teacherlist.Add(t427);
            teacherclass t428 = new teacherclass(); t428.name = "Gunnel Frid"; t428.firstname = "Gunnel"; t428.lastname = "Frid"; t428.teacherID = "gfi"; t428.birthday = "470619"; teacherlist.Add(t428);
            teacherclass t429 = new teacherclass(); t429.name = "Gergö Gyulai"; t429.firstname = "Gergö"; t429.lastname = "Gyulai"; t429.teacherID = "ggy"; t429.birthday = "840203"; teacherlist.Add(t429);
            teacherclass t430 = new teacherclass(); t430.name = "Göran Hultgren"; t430.firstname = "Göran"; t430.lastname = "Hultgren"; t430.teacherID = "ghu"; t430.birthday = "600721"; teacherlist.Add(t430);
            teacherclass t431 = new teacherclass(); t431.name = "Gunnar Isaksson"; t431.firstname = "Gunnar"; t431.lastname = "Isaksson"; t431.teacherID = "gis"; t431.birthday = "640622"; teacherlist.Add(t431);
            teacherclass t432 = new teacherclass(); t432.name = "Göran Johansson"; t432.firstname = "Göran"; t432.lastname = "Johansson"; t432.teacherID = "gjo"; t432.birthday = "520625"; teacherlist.Add(t432);
            teacherclass t433 = new teacherclass(); t433.name = "Gun Karlsson"; t433.firstname = "Gun"; t433.lastname = "Karlsson"; t433.teacherID = "gka"; t433.birthday = "420217"; teacherlist.Add(t433);
            teacherclass t434 = new teacherclass(); t434.name = "Glenn Kallio"; t434.firstname = "Glenn"; t434.lastname = "Kallio"; t434.teacherID = "gkl"; t434.birthday = "521204"; teacherlist.Add(t434);
            teacherclass t435 = new teacherclass(); t435.name = "Göran Land"; t435.firstname = "Göran"; t435.lastname = "Land"; t435.teacherID = "gla"; t435.birthday = "640916"; teacherlist.Add(t435);
            teacherclass t436 = new teacherclass(); t436.name = "Gunilla Lindqvist"; t436.firstname = "Gunilla"; t436.lastname = "Lindqvist"; t436.teacherID = "gln"; t436.birthday = "680812"; teacherlist.Add(t436);
            teacherclass t437 = new teacherclass(); t437.name = "Gina Larsson"; t437.firstname = "Gina"; t437.lastname = "Larsson"; t437.teacherID = "glr"; t437.birthday = "580516"; teacherlist.Add(t437);
            teacherclass t438 = new teacherclass(); t438.name = "Giulia Messina Dahlberg"; t438.firstname = "Giulia"; t438.lastname = "Messina Dahlberg"; t438.teacherID = "gme"; t438.birthday = "790225"; teacherlist.Add(t438);
            teacherclass t439 = new teacherclass(); t439.name = "Göran Morén"; t439.firstname = "Göran"; t439.lastname = "Morén"; t439.teacherID = "gmo"; t439.birthday = "581213"; teacherlist.Add(t439);
            teacherclass t440 = new teacherclass(); t440.name = "Gerard Moroney"; t440.firstname = "Gerard"; t440.lastname = "Moroney"; t440.teacherID = "gmr"; t440.birthday = "670308"; teacherlist.Add(t440);
            teacherclass t441 = new teacherclass(); t441.name = "Gunn Nyberg"; t441.firstname = "Gunn"; t441.lastname = "Nyberg"; t441.teacherID = "gny"; t441.birthday = "580704"; teacherlist.Add(t441);
            teacherclass t442 = new teacherclass(); t442.name = "Gunilla Oscarsdotter"; t442.firstname = "Gunilla"; t442.lastname = "Oscarsdotter"; t442.teacherID = "gos"; t442.birthday = "540429"; teacherlist.Add(t442);
            teacherclass t443 = new teacherclass(); t443.name = "Gunnar Rosén"; t443.firstname = "Gunnar"; t443.lastname = "Rosén"; t443.teacherID = "grs"; t443.birthday = "470406"; teacherlist.Add(t443);
            teacherclass t444 = new teacherclass(); t444.name = "Ginger Selander"; t444.firstname = "Ginger"; t444.lastname = "Selander"; t444.teacherID = "gse"; t444.birthday = "510701"; teacherlist.Add(t444);
            teacherclass t445 = new teacherclass(); t445.name = "Gudmund Stintzing"; t445.firstname = "Gudmund"; t445.lastname = "Stintzing"; t445.teacherID = "gsn"; t445.birthday = "431228"; teacherlist.Add(t445);
            teacherclass t446 = new teacherclass(); t446.name = "Gusten Stenlund"; t446.firstname = "Gusten"; t446.lastname = "Stenlund"; t446.teacherID = "gst"; t446.birthday = "530122"; teacherlist.Add(t446);
            teacherclass t447 = new teacherclass(); t447.name = "Gunnar Ternhag"; t447.firstname = "Gunnar"; t447.lastname = "Ternhag"; t447.teacherID = "gte"; t447.birthday = "481031"; teacherlist.Add(t447);
            teacherclass t448 = new teacherclass(); t448.name = "Gull Törnegren"; t448.firstname = "Gull"; t448.lastname = "Törnegren"; t448.teacherID = "gto"; t448.birthday = "550709"; teacherlist.Add(t448);
            teacherclass t449 = new teacherclass(); t449.name = "Gisela Vilhelmsdotter"; t449.firstname = "Gisela"; t449.lastname = "Vilhelmsdotter"; t449.teacherID = "gwi"; t449.birthday = "440804"; teacherlist.Add(t449);
            teacherclass t450 = new teacherclass(); t450.name = "Gun-Marie Wetso"; t450.firstname = "Gun-Marie"; t450.lastname = "Wetso"; t450.teacherID = "gwt"; t450.birthday = "551107"; teacherlist.Add(t450);
            teacherclass t451 = new teacherclass(); t451.name = "Henrik Åström Elmersjö"; t451.firstname = "Henrik"; t451.lastname = "Åström Elmersjö"; t451.teacherID = "hae"; t451.birthday = "780125"; teacherlist.Add(t451);
            teacherclass t452 = new teacherclass(); t452.name = "Helena Alm"; t452.firstname = "Helena"; t452.lastname = "Alm"; t452.teacherID = "hal"; t452.birthday = "670809"; teacherlist.Add(t452);
            teacherclass t453 = new teacherclass(); t453.name = "Henrik Aminoff"; t453.firstname = "Henrik"; t453.lastname = "Aminoff"; t453.teacherID = "ham"; t453.birthday = "830301"; teacherlist.Add(t453);
            teacherclass t454 = new teacherclass(); t454.name = "Henrik Åsenlund"; t454.firstname = "Henrik"; t454.lastname = "Åsenlund"; t454.teacherID = "has"; t454.birthday = "730123"; teacherlist.Add(t454);
            teacherclass t455 = new teacherclass(); t455.name = "Helena Bellardini"; t455.firstname = "Helena"; t455.lastname = "Bellardini"; t455.teacherID = "hbe"; t455.birthday = "621223"; teacherlist.Add(t455);
            teacherclass t456 = new teacherclass(); t456.name = "Håkan Björklund"; t456.firstname = "Håkan"; t456.lastname = "Björklund"; t456.teacherID = "hbj"; t456.birthday = "570504"; teacherlist.Add(t456);
            teacherclass t457 = new teacherclass(); t457.name = "Hans Danelid"; t457.firstname = "Hans"; t457.lastname = "Danelid"; t457.teacherID = "hda"; t457.birthday = "550917"; teacherlist.Add(t457);
            teacherclass t458 = new teacherclass(); t458.name = "Helena Danielsson"; t458.firstname = "Helena"; t458.lastname = "Danielsson"; t458.teacherID = "hdn"; t458.birthday = "470922"; teacherlist.Add(t458);
            teacherclass t459 = new teacherclass(); t459.name = "Hayri Dündar"; t459.firstname = "Hayri"; t459.lastname = "Dündar"; t459.teacherID = "hdu"; t459.birthday = "910804"; teacherlist.Add(t459);
            teacherclass t460 = new teacherclass(); t460.name = "Hugues Engel"; t460.firstname = "Hugues"; t460.lastname = "Engel"; t460.teacherID = "hee"; t460.birthday = "740311"; teacherlist.Add(t460);
            teacherclass t461 = new teacherclass(); t461.name = "Hanna Efrik"; t461.firstname = "Hanna"; t461.lastname = "Efrik"; t461.teacherID = "hef"; t461.birthday = "830411"; teacherlist.Add(t461);
            teacherclass t462 = new teacherclass(); t462.name = "Hans Englund"; t462.firstname = "Hans"; t462.lastname = "Englund"; t462.teacherID = "heg"; t462.birthday = "711007"; teacherlist.Add(t462);
            teacherclass t463 = new teacherclass(); t463.name = "Hans-Erik Hellborg"; t463.firstname = "Hans-Erik"; t463.lastname = "Hellborg"; t463.teacherID = "heh"; t463.birthday = "490616"; teacherlist.Add(t463);
            teacherclass t464 = new teacherclass(); t464.name = "Hans-Edy Mårtensson"; t464.firstname = "Hans-Edy"; t464.lastname = "Mårtensson"; t464.teacherID = "hem"; t464.birthday = "550512"; teacherlist.Add(t464);
            teacherclass t465 = new teacherclass(); t465.name = "Henrik Englund"; t465.firstname = "Henrik"; t465.lastname = "Englund"; t465.teacherID = "hen"; t465.birthday = "640614"; teacherlist.Add(t465);
            teacherclass t466 = new teacherclass(); t466.name = "Hans Ersson"; t466.firstname = "Hans"; t466.lastname = "Ersson"; t466.teacherID = "her"; t466.birthday = "591225"; teacherlist.Add(t466);
            teacherclass t467 = new teacherclass(); t467.name = "Helena Fridberg"; t467.firstname = "Helena"; t467.lastname = "Fridberg"; t467.teacherID = "hfi"; t467.birthday = "691006"; teacherlist.Add(t467);
            teacherclass t468 = new teacherclass(); t468.name = "Hasan Fleyeh"; t468.firstname = "Hasan"; t468.lastname = "Fleyeh"; t468.teacherID = "hfl"; t468.birthday = "580808"; teacherlist.Add(t468);
            teacherclass t469 = new teacherclass(); t469.name = "Henrietta Forsman"; t469.firstname = "Henrietta"; t469.lastname = "Forsman"; t469.teacherID = "hfo"; t469.birthday = "800214"; teacherlist.Add(t469);
            teacherclass t470 = new teacherclass(); t470.name = "Hannes Forsell"; t470.firstname = "Hannes"; t470.lastname = "Forsell"; t470.teacherID = "hfr"; t470.birthday = "771011"; teacherlist.Add(t470);
            teacherclass t471 = new teacherclass(); t471.name = "Helena Grundén"; t471.firstname = "Helena"; t471.lastname = "Grundén"; t471.teacherID = "hgn"; t471.birthday = "681120"; teacherlist.Add(t471);
            teacherclass t472 = new teacherclass(); t472.name = "Hans Gustafsson"; t472.firstname = "Hans"; t472.lastname = "Gustafsson"; t472.teacherID = "hgu"; t472.birthday = "521217"; teacherlist.Add(t472);
            teacherclass t473 = new teacherclass(); t473.name = "Hanna Hodacs"; t473.firstname = "Hanna"; t473.lastname = "Hodacs"; t473.teacherID = "hhd"; t473.birthday = "710914"; teacherlist.Add(t473);
            teacherclass t474 = new teacherclass(); t474.name = "Håkan Holmberg"; t474.firstname = "Håkan"; t474.lastname = "Holmberg"; t474.teacherID = "hhm"; t474.birthday = "570826"; teacherlist.Add(t474);
            teacherclass t475 = new teacherclass(); t475.name = "Helén Holmquist"; t475.firstname = "Helén"; t475.lastname = "Holmquist"; t475.teacherID = "hho"; t475.birthday = "630822"; teacherlist.Add(t475);
            teacherclass t476 = new teacherclass(); t476.name = "Hajo Holtz"; t476.firstname = "Hajo"; t476.lastname = "Holtz"; t476.teacherID = "hht"; t476.birthday = "810804"; teacherlist.Add(t476);
            teacherclass t477 = new teacherclass(); t477.name = "Hao Huang"; t477.firstname = "Hao"; t477.lastname = "Huang"; t477.teacherID = "hhu"; t477.birthday = "880505"; teacherlist.Add(t477);
            teacherclass t478 = new teacherclass(); t478.name = "Hiroko Inose"; t478.firstname = "Hiroko"; t478.lastname = "Inose"; t478.teacherID = "hin"; t478.birthday = "720212"; teacherlist.Add(t478);
            teacherclass t479 = new teacherclass(); t479.name = "Hannes Jardbrink"; t479.firstname = "Hannes"; t479.lastname = "Jardbrink"; t479.teacherID = "hja"; t479.birthday = "940305"; teacherlist.Add(t479);
            teacherclass t480 = new teacherclass(); t480.name = "Hans Jernberg"; t480.firstname = "Hans"; t480.lastname = "Jernberg"; t480.teacherID = "hje"; t480.birthday = "641116"; teacherlist.Add(t480);
            teacherclass t481 = new teacherclass(); t481.name = "Henrik Janols"; t481.firstname = "Henrik"; t481.lastname = "Janols"; t481.teacherID = "hjl"; t481.birthday = "760215"; teacherlist.Add(t481);
            teacherclass t482 = new teacherclass(); t482.name = "Herbert Jonsson"; t482.firstname = "Herbert"; t482.lastname = "Jonsson"; t482.teacherID = "hjn"; t482.birthday = "620306"; teacherlist.Add(t482);
            teacherclass t483 = new teacherclass(); t483.name = "Hans Jones"; t483.firstname = "Hans"; t483.lastname = "Jones"; t483.teacherID = "hjo"; t483.birthday = "660913"; teacherlist.Add(t483);
            teacherclass t484 = new teacherclass(); t484.name = "Hannele Junkala"; t484.firstname = "Hannele"; t484.lastname = "Junkala"; t484.teacherID = "hju"; t484.birthday = "630423"; teacherlist.Add(t484);
            teacherclass t485 = new teacherclass(); t485.name = "Henrik Carlsson"; t485.firstname = "Henrik"; t485.lastname = "Carlsson"; t485.teacherID = "hka"; t485.birthday = "851216"; teacherlist.Add(t485);
            teacherclass t486 = new teacherclass(); t486.name = "Hans Kellner"; t486.firstname = "Hans"; t486.lastname = "Kellner"; t486.teacherID = "hke"; t486.birthday = "891126"; teacherlist.Add(t486);
            teacherclass t487 = new teacherclass(); t487.name = "Helena Kåks"; t487.firstname = "Helena"; t487.lastname = "Kåks"; t487.teacherID = "hkk"; t487.birthday = "630514"; teacherlist.Add(t487);
            teacherclass t488 = new teacherclass(); t488.name = "Hed Kerstin Larsson"; t488.firstname = "Hed Kerstin"; t488.lastname = "Larsson"; t488.teacherID = "hkl"; t488.birthday = "580915"; teacherlist.Add(t488);
            teacherclass t489 = new teacherclass(); t489.name = "Håkan Larsson"; t489.firstname = "Håkan"; t489.lastname = "Larsson"; t489.teacherID = "hla"; t489.birthday = "670524"; teacherlist.Add(t489);
            teacherclass t490 = new teacherclass(); t490.name = "Henning Lindblad"; t490.firstname = "Henning"; t490.lastname = "Lindblad"; t490.teacherID = "hld"; t490.birthday = "690803"; teacherlist.Add(t490);
            teacherclass t491 = new teacherclass(); t491.name = "Helena Lindgren"; t491.firstname = "Helena"; t491.lastname = "Lindgren"; t491.teacherID = "hli"; t491.birthday = "650618"; teacherlist.Add(t491);
            teacherclass t492 = new teacherclass(); t492.name = "Helena Limér"; t492.firstname = "Helena"; t492.lastname = "Limér"; t492.teacherID = "hlm"; t492.birthday = "910731"; teacherlist.Add(t492);
            teacherclass t493 = new teacherclass(); t493.name = "Hans Lundh"; t493.firstname = "Hans"; t493.lastname = "Lundh"; t493.teacherID = "hln"; t493.birthday = "380911"; teacherlist.Add(t493);
            teacherclass t494 = new teacherclass(); t494.name = "Hans Lundkvist"; t494.firstname = "Hans"; t494.lastname = "Lundkvist"; t494.teacherID = "hlu"; t494.birthday = "560829"; teacherlist.Add(t494);
            teacherclass t495 = new teacherclass(); t495.name = "Hema Yella"; t495.firstname = "Hema"; t495.lastname = "Yella"; t495.teacherID = "hmy"; t495.birthday = "800820"; teacherlist.Add(t495);
            teacherclass t496 = new teacherclass(); t496.name = "Helena Nilsson"; t496.firstname = "Helena"; t496.lastname = "Nilsson"; t496.teacherID = "hni"; t496.birthday = "831104"; teacherlist.Add(t496);
            teacherclass t497 = new teacherclass(); t497.name = "Henrik Nordin"; t497.firstname = "Henrik"; t497.lastname = "Nordin"; t497.teacherID = "hno"; t497.birthday = "680528"; teacherlist.Add(t497);
            teacherclass t498 = new teacherclass(); t498.name = "Helén Olsson"; t498.firstname = "Helén"; t498.lastname = "Olsson"; t498.teacherID = "hol"; t498.birthday = "610215"; teacherlist.Add(t498);
            teacherclass t499 = new teacherclass(); t499.name = "Tony Mattsson"; t499.firstname = "Tony"; t499.lastname = "Mattsson"; t499.teacherID = "hom"; t499.birthday = "720915"; teacherlist.Add(t499);
            teacherclass t500 = new teacherclass(); t500.name = "Hans Persson"; t500.firstname = "Hans"; t500.lastname = "Persson"; t500.teacherID = "hpe"; t500.birthday = "631001"; teacherlist.Add(t500);
            teacherclass t501 = new teacherclass(); t501.name = "Honour Pearson"; t501.firstname = "Honour"; t501.lastname = "Pearson"; t501.teacherID = "hpr"; t501.birthday = "710619"; teacherlist.Add(t501);
            teacherclass t502 = new teacherclass(); t502.name = "Hanna Randell"; t502.firstname = "Hanna"; t502.lastname = "Randell"; t502.teacherID = "hrn"; t502.birthday = "921104"; teacherlist.Add(t502);
            teacherclass t503 = new teacherclass(); t503.name = "Hans Rosendahl"; t503.firstname = "Hans"; t503.lastname = "Rosendahl"; t503.teacherID = "hro"; t503.birthday = "560126"; teacherlist.Add(t503);
            teacherclass t504 = new teacherclass(); t504.name = "Hans Söderström"; t504.firstname = "Hans"; t504.lastname = "Söderström"; t504.teacherID = "hsd"; t504.birthday = "580518"; teacherlist.Add(t504);
            teacherclass t505 = new teacherclass(); t505.name = "Helen Sterner"; t505.firstname = "Helen"; t505.lastname = "Sterner"; t505.teacherID = "hse"; t505.birthday = "690419"; teacherlist.Add(t505);
            teacherclass t506 = new teacherclass(); t506.name = "Harriet Sjögren"; t506.firstname = "Harriet"; t506.lastname = "Sjögren"; t506.teacherID = "hsg"; t506.birthday = "471017"; teacherlist.Add(t506);
            teacherclass t507 = new teacherclass(); t507.name = "Henrik Smångs"; t507.firstname = "Henrik"; t507.lastname = "Smångs"; t507.teacherID = "hsm"; t507.birthday = "760119"; teacherlist.Add(t507);
            teacherclass t508 = new teacherclass(); t508.name = "Hamzah Ssemakula"; t508.firstname = "Hamzah"; t508.lastname = "Ssemakula"; t508.teacherID = "hss"; t508.birthday = "600912"; teacherlist.Add(t508);
            teacherclass t509 = new teacherclass(); t509.name = "Henrik Stub"; t509.firstname = "Henrik"; t509.lastname = "Stub"; t509.teacherID = "hsu"; t509.birthday = "640929"; teacherlist.Add(t509);
            teacherclass t510 = new teacherclass(); t510.name = "Harald Svedung"; t510.firstname = "Harald"; t510.lastname = "Svedung"; t510.teacherID = "hsv"; t510.birthday = "700610"; teacherlist.Add(t510);
            teacherclass t511 = new teacherclass(); t511.name = "Hanna Trotzig"; t511.firstname = "Hanna"; t511.lastname = "Trotzig"; t511.teacherID = "htr"; t511.birthday = "670820"; teacherlist.Add(t511);
            teacherclass t512 = new teacherclass(); t512.name = "Helena Wikström"; t512.firstname = "Helena"; t512.lastname = "Wikström"; t512.teacherID = "hwi"; t512.birthday = "640317"; teacherlist.Add(t512);
            teacherclass t513 = new teacherclass(); t513.name = "Helena Willman"; t513.firstname = "Helena"; t513.lastname = "Willman"; t513.teacherID = "hwl"; t513.birthday = "721104"; teacherlist.Add(t513);
            teacherclass t514 = new teacherclass(); t514.name = "Holger Weiss"; t514.firstname = "Holger"; t514.lastname = "Weiss"; t514.teacherID = "hws"; t514.birthday = "660385"; teacherlist.Add(t514);
            teacherclass t515 = new teacherclass(); t515.name = "Ingvar Bergh"; t515.firstname = "Ingvar"; t515.lastname = "Bergh"; t515.teacherID = "ibe"; t515.birthday = "440821"; teacherlist.Add(t515);
            teacherclass t516 = new teacherclass(); t516.name = "Ina Carlsson"; t516.firstname = "Ina"; t516.lastname = "Carlsson"; t516.teacherID = "icr"; t516.birthday = "910808"; teacherlist.Add(t516);
            teacherclass t517 = new teacherclass(); t517.name = "Ida Dahlström"; t517.firstname = "Ida"; t517.lastname = "Dahlström"; t517.teacherID = "ida"; t517.birthday = "860131"; teacherlist.Add(t517);
            teacherclass t518 = new teacherclass(); t518.name = "Isabel De La Cuesta"; t518.firstname = "Isabel De La"; t518.lastname = "Cuesta"; t518.teacherID = "idc"; t518.birthday = "591222"; teacherlist.Add(t518);
            teacherclass t519 = new teacherclass(); t519.name = "Ioanna Farsari"; t519.firstname = "Ioanna"; t519.lastname = "Farsari"; t519.teacherID = "ifa"; t519.birthday = "710925"; teacherlist.Add(t519);
            teacherclass t520 = new teacherclass(); t520.name = "Ingrid From"; t520.firstname = "Ingrid"; t520.lastname = "From"; t520.teacherID = "ifr"; t520.birthday = "530906"; teacherlist.Add(t520);
            teacherclass t521 = new teacherclass(); t521.name = "Irene Gilsenan Nordin"; t521.firstname = "Irene"; t521.lastname = "Gilsenan Nordin"; t521.teacherID = "ign"; t521.birthday = "471206"; teacherlist.Add(t521);
            teacherclass t522 = new teacherclass(); t522.name = "Ingrid Grosse"; t522.firstname = "Ingrid"; t522.lastname = "Grosse"; t522.teacherID = "igr"; t522.birthday = "710210"; teacherlist.Add(t522);
            teacherclass t523 = new teacherclass(); t523.name = "Inger Hermansson"; t523.firstname = "Inger"; t523.lastname = "Hermansson"; t523.teacherID = "ihe"; t523.birthday = "570107"; teacherlist.Add(t523);
            teacherclass t524 = new teacherclass(); t524.name = "Ingela Johansson"; t524.firstname = "Ingela"; t524.lastname = "Johansson"; t524.teacherID = "ijo"; t524.birthday = "710114"; teacherlist.Add(t524);
            teacherclass t525 = new teacherclass(); t525.name = "Ida Josefsson"; t525.firstname = "Ida"; t525.lastname = "Josefsson"; t525.teacherID = "ijs"; t525.birthday = "850922"; teacherlist.Add(t525);
            teacherclass t526 = new teacherclass(); t526.name = "Inge Karlsson"; t526.firstname = "Inge"; t526.lastname = "Karlsson"; t526.teacherID = "ika"; t526.birthday = "531113"; teacherlist.Add(t526);
            teacherclass t527 = new teacherclass(); t527.name = "Irina Karlsohn"; t527.firstname = "Irina"; t527.lastname = "Karlsohn"; t527.teacherID = "ikr"; t527.birthday = "760420"; teacherlist.Add(t527);
            teacherclass t528 = new teacherclass(); t528.name = "Inger Lindqvist"; t528.firstname = "Inger"; t528.lastname = "Lindqvist"; t528.teacherID = "ili"; t528.birthday = "710129"; teacherlist.Add(t528);
            teacherclass t529 = new teacherclass(); t529.name = "Inger Lundberg Santesson"; t529.firstname = "Inger"; t529.lastname = "Lundberg Santesson"; t529.teacherID = "ilu"; t529.birthday = "591212"; teacherlist.Add(t529);
            teacherclass t530 = new teacherclass(); t530.name = "Ing-Marie Andersson"; t530.firstname = "Ing-Marie"; t530.lastname = "Andersson"; t530.teacherID = "ima"; t530.birthday = "540316"; teacherlist.Add(t530);
            teacherclass t531 = new teacherclass(); t531.name = "Inger Midmo"; t531.firstname = "Inger"; t531.lastname = "Midmo"; t531.teacherID = "imi"; t531.birthday = "571124"; teacherlist.Add(t531);
            teacherclass t532 = new teacherclass(); t532.name = "Maria Stigsdotter"; t532.firstname = "Maria"; t532.lastname = "Stigsdotter"; t532.teacherID = "ims"; t532.birthday = "790212"; teacherlist.Add(t532);
            teacherclass t533 = new teacherclass(); t533.name = "Inger Nylander"; t533.firstname = "Inger"; t533.lastname = "Nylander"; t533.teacherID = "inr"; t533.birthday = "560621"; teacherlist.Add(t533);
            teacherclass t534 = new teacherclass(); t534.name = "Ingemar Nygren"; t534.firstname = "Ingemar"; t534.lastname = "Nygren"; t534.teacherID = "iny"; t534.birthday = "520409"; teacherlist.Add(t534);
            teacherclass t535 = new teacherclass(); t535.name = "Ibro Ribic"; t535.firstname = "Ibro"; t535.lastname = "Ribic"; t535.teacherID = "irb"; t535.birthday = "860609"; teacherlist.Add(t535);
            teacherclass t536 = new teacherclass(); t536.name = "Iris Ridder"; t536.firstname = "Iris"; t536.lastname = "Ridder"; t536.teacherID = "iri"; t536.birthday = "660727"; teacherlist.Add(t536);
            teacherclass t537 = new teacherclass(); t537.name = "Ingela Spegel Nääs"; t537.firstname = "Ingela"; t537.lastname = "Spegel Nääs"; t537.teacherID = "isp"; t537.birthday = "590616"; teacherlist.Add(t537);
            teacherclass t538 = new teacherclass(); t538.name = "Ingegerd Strömkvist"; t538.firstname = "Ingegerd"; t538.lastname = "Strömkvist"; t538.teacherID = "ist"; t538.birthday = "520119"; teacherlist.Add(t538);
            teacherclass t539 = new teacherclass(); t539.name = "Ingrid Svensson"; t539.firstname = "Ingrid"; t539.lastname = "Svensson"; t539.teacherID = "isv"; t539.birthday = "640813"; teacherlist.Add(t539);
            teacherclass t540 = new teacherclass(); t540.name = "Ilias Thomas"; t540.firstname = "Ilias"; t540.lastname = "Thomas"; t540.teacherID = "ith"; t540.birthday = "870726"; teacherlist.Add(t540);
            teacherclass t541 = new teacherclass(); t541.name = "Irma Vaarala"; t541.firstname = "Irma"; t541.lastname = "Vaarala"; t541.teacherID = "iva"; t541.birthday = "570302"; teacherlist.Add(t541);
            teacherclass t542 = new teacherclass(); t542.name = "Ina Amalia Von Schantz Lundgren"; t542.firstname = "Ina Amalia Von"; t542.lastname = "Schantz Lundgren"; t542.teacherID = "ivo"; t542.birthday = "580227"; teacherlist.Add(t542);
            teacherclass t543 = new teacherclass(); t543.name = "Jenny Åberg"; t543.firstname = "Jenny"; t543.lastname = "Åberg"; t543.teacherID = "jae"; t543.birthday = "780215"; teacherlist.Add(t543);
            teacherclass t544 = new teacherclass(); t544.name = "Jan Åkerstedt"; t544.firstname = "Jan"; t544.lastname = "Åkerstedt"; t544.teacherID = "jak"; t544.birthday = "521019"; teacherlist.Add(t544);
            teacherclass t545 = new teacherclass(); t545.name = "Jonn Are Myhren"; t545.firstname = "Jonn Are"; t545.lastname = "Myhren"; t545.teacherID = "jam"; t545.birthday = "790703"; teacherlist.Add(t545);
            teacherclass t546 = new teacherclass(); t546.name = "Johan Ärnlöv"; t546.firstname = "Johan"; t546.lastname = "Ärnlöv"; t546.teacherID = "jan"; t546.birthday = "701008"; teacherlist.Add(t546);
            teacherclass t547 = new teacherclass(); t547.name = "Jan Olsson"; t547.firstname = "Jan"; t547.lastname = "Olsson"; t547.teacherID = "jao"; t547.birthday = "640526"; teacherlist.Add(t547);
            teacherclass t548 = new teacherclass(); t548.name = "Jannika Persson"; t548.firstname = "Jannika"; t548.lastname = "Persson"; t548.teacherID = "jap"; t548.birthday = "830901"; teacherlist.Add(t548);
            teacherclass t549 = new teacherclass(); t549.name = "Johan Åkerstedt"; t549.firstname = "Johan"; t549.lastname = "Åkerstedt"; t549.teacherID = "jar"; t549.birthday = "731218"; teacherlist.Add(t549);
            teacherclass t550 = new teacherclass(); t550.name = "Joachim Bergenstråhle"; t550.firstname = "Joachim"; t550.lastname = "Bergenstråhle"; t550.teacherID = "jbe"; t550.birthday = "580626"; teacherlist.Add(t550);
            teacherclass t551 = new teacherclass(); t551.name = "Johanna Björs Hansols"; t551.firstname = "Johanna"; t551.lastname = "Björs Hansols"; t551.teacherID = "jbh"; t551.birthday = "660507"; teacherlist.Add(t551);
            teacherclass t552 = new teacherclass(); t552.name = "Jan Bjelvenmark"; t552.firstname = "Jan"; t552.lastname = "Bjelvenmark"; t552.teacherID = "jbl"; t552.birthday = "610911"; teacherlist.Add(t552);
            teacherclass t553 = new teacherclass(); t553.name = "Jesper Brunnberg"; t553.firstname = "Jesper"; t553.lastname = "Brunnberg"; t553.teacherID = "jbu"; t553.birthday = "781219"; teacherlist.Add(t553);
            teacherclass t554 = new teacherclass(); t554.name = "Josefina Cederberg"; t554.firstname = "Josefina"; t554.lastname = "Cederberg"; t554.teacherID = "jce"; t554.birthday = "911025"; teacherlist.Add(t554);
            teacherclass t555 = new teacherclass(); t555.name = "Joel Dittrich"; t555.firstname = "Joel"; t555.lastname = "Dittrich"; t555.teacherID = "jdt"; t555.birthday = "820629"; teacherlist.Add(t555);
            teacherclass t556 = new teacherclass(); t556.name = "Jenny Svärd"; t556.firstname = "Jenny"; t556.lastname = "Svärd"; t556.teacherID = "jea"; t556.birthday = "891118"; teacherlist.Add(t556);
            teacherclass t557 = new teacherclass(); t557.name = "Jonas Edberg"; t557.firstname = "Jonas"; t557.lastname = "Edberg"; t557.teacherID = "jed"; t557.birthday = "591220"; teacherlist.Add(t557);
            teacherclass t558 = new teacherclass(); t558.name = "Jennie Svensson"; t558.firstname = "Jennie"; t558.lastname = "Svensson"; t558.teacherID = "jee"; t558.birthday = "870415"; teacherlist.Add(t558);
            teacherclass t559 = new teacherclass(); t559.name = "Jenny Ehnberg"; t559.firstname = "Jenny"; t559.lastname = "Ehnberg"; t559.teacherID = "jeh"; t559.birthday = "840927"; teacherlist.Add(t559);
            teacherclass t560 = new teacherclass(); t560.name = "Jörgen Elbe"; t560.firstname = "Jörgen"; t560.lastname = "Elbe"; t560.teacherID = "jel"; t560.birthday = "590708"; teacherlist.Add(t560);
            teacherclass t561 = new teacherclass(); t561.name = "Joakim Erdman Sund"; t561.firstname = "Joakim"; t561.lastname = "Erdman Sund"; t561.teacherID = "jem"; t561.birthday = "751120"; teacherlist.Add(t561);
            teacherclass t562 = new teacherclass(); t562.name = "Jesper Engström"; t562.firstname = "Jesper"; t562.lastname = "Engström"; t562.teacherID = "jen"; t562.birthday = "860622"; teacherlist.Add(t562);
            teacherclass t563 = new teacherclass(); t563.name = "Jens Evaldsson"; t563.firstname = "Jens"; t563.lastname = "Evaldsson"; t563.teacherID = "jev"; t563.birthday = "750501"; teacherlist.Add(t563);
            teacherclass t564 = new teacherclass(); t564.name = "Jörgen Fältsjö"; t564.firstname = "Jörgen"; t564.lastname = "Fältsjö"; t564.teacherID = "jfa"; t564.birthday = "641121"; teacherlist.Add(t564);
            teacherclass t565 = new teacherclass(); t565.name = "Jenny Fritz"; t565.firstname = "Jenny"; t565.lastname = "Fritz"; t565.teacherID = "jfi"; t565.birthday = "821226"; teacherlist.Add(t565);
            teacherclass t566 = new teacherclass(); t566.name = "Jerome-Frederic Josserand"; t566.firstname = "Jerome-Frederic"; t566.lastname = "Josserand"; t566.teacherID = "jfj"; t566.birthday = "640122"; teacherlist.Add(t566);
            teacherclass t567 = new teacherclass(); t567.name = "Jan Florin"; t567.firstname = "Jan"; t567.lastname = "Florin"; t567.teacherID = "jfl"; t567.birthday = "560929"; teacherlist.Add(t567);
            teacherclass t568 = new teacherclass(); t568.name = "Johan Frodell"; t568.firstname = "Johan"; t568.lastname = "Frodell"; t568.teacherID = "jfo"; t568.birthday = "750326"; teacherlist.Add(t568);
            teacherclass t569 = new teacherclass(); t569.name = "Jan Fredriksson"; t569.firstname = "Jan"; t569.lastname = "Fredriksson"; t569.teacherID = "jfr"; t569.birthday = "530514"; teacherlist.Add(t569);
            teacherclass t570 = new teacherclass(); t570.name = "Joachim Forsberg"; t570.firstname = "Joachim"; t570.lastname = "Forsberg"; t570.teacherID = "jfs"; t570.birthday = "720423"; teacherlist.Add(t570);
            teacherclass t571 = new teacherclass(); t571.name = "Jessica Gyhlesten Back"; t571.firstname = "Jessica"; t571.lastname = "Gyhlesten Back"; t571.teacherID = "jgb"; t571.birthday = "820511"; teacherlist.Add(t571);
            teacherclass t572 = new teacherclass(); t572.name = "Jeff Gracie"; t572.firstname = "Jeff"; t572.lastname = "Gracie"; t572.teacherID = "jgc"; t572.birthday = "710309"; teacherlist.Add(t572);
            teacherclass t573 = new teacherclass(); t573.name = "Johan Gregeby"; t573.firstname = "Johan"; t573.lastname = "Gregeby"; t573.teacherID = "jge"; t573.birthday = "810717"; teacherlist.Add(t573);
            teacherclass t574 = new teacherclass(); t574.name = "Johanna Gustafsson-Lundberg"; t574.firstname = "Johanna"; t574.lastname = "Gustafsson-Lundberg"; t574.teacherID = "jgu"; t574.birthday = "690218"; teacherlist.Add(t574);
            teacherclass t575 = new teacherclass(); t575.name = "Jürgen Hartmann"; t575.firstname = "Jürgen"; t575.lastname = "Hartmann"; t575.teacherID = "jha"; t575.birthday = "440318"; teacherlist.Add(t575);
            teacherclass t576 = new teacherclass(); t576.name = "Johanna Hedberg Nilsson"; t576.firstname = "Johanna"; t576.lastname = "Hedberg Nilsson"; t576.teacherID = "jhb"; t576.birthday = "760619"; teacherlist.Add(t576);
            teacherclass t577 = new teacherclass(); t577.name = "Johan Hedman"; t577.firstname = "Johan"; t577.lastname = "Hedman"; t577.teacherID = "jhd"; t577.birthday = "770615"; teacherlist.Add(t577);
            teacherclass t578 = new teacherclass(); t578.name = "Johan Heier"; t578.firstname = "Johan"; t578.lastname = "Heier"; t578.teacherID = "jhe"; t578.birthday = "830421"; teacherlist.Add(t578);
            teacherclass t579 = new teacherclass(); t579.name = "Jenny Hjelm"; t579.firstname = "Jenny"; t579.lastname = "Hjelm"; t579.teacherID = "jhj"; t579.birthday = "720729"; teacherlist.Add(t579);
            teacherclass t580 = new teacherclass(); t580.name = "Johan Håkansson"; t580.firstname = "Johan"; t580.lastname = "Håkansson"; t580.teacherID = "jhk"; t580.birthday = "660125"; teacherlist.Add(t580);
            teacherclass t581 = new teacherclass(); t581.name = "Jan-Ola Högberg"; t581.firstname = "Jan-Ola"; t581.lastname = "Högberg"; t581.teacherID = "jho"; t581.birthday = "470222"; teacherlist.Add(t581);
            teacherclass t582 = new teacherclass(); t582.name = "Joakim Hermansson"; t582.firstname = "Joakim"; t582.lastname = "Hermansson"; t582.teacherID = "jhr"; t582.birthday = "641104"; teacherlist.Add(t582);
            teacherclass t583 = new teacherclass(); t583.name = "Jill Sirén"; t583.firstname = "Jill"; t583.lastname = "Sirén"; t583.teacherID = "jii"; t583.birthday = "760817"; teacherlist.Add(t583);
            teacherclass t584 = new teacherclass(); t584.name = "Janne Paavilainen"; t584.firstname = "Janne"; t584.lastname = "Paavilainen"; t584.teacherID = "jip"; t584.birthday = "730612"; teacherlist.Add(t584);
            teacherclass t585 = new teacherclass(); t585.name = "Jenny Isberg"; t585.firstname = "Jenny"; t585.lastname = "Isberg"; t585.teacherID = "jis"; t585.birthday = "710716"; teacherlist.Add(t585);
            teacherclass t586 = new teacherclass(); t586.name = "Jonas Jäder"; t586.firstname = "Jonas"; t586.lastname = "Jäder"; t586.teacherID = "jjd"; t586.birthday = "710713"; teacherlist.Add(t586);
            teacherclass t587 = new teacherclass(); t587.name = "Jimmy Johansson"; t587.firstname = "Jimmy"; t587.lastname = "Johansson"; t587.teacherID = "jjh"; t587.birthday = "760327"; teacherlist.Add(t587);
            teacherclass t588 = new teacherclass(); t588.name = "Jesper Jonsson"; t588.firstname = "Jesper"; t588.lastname = "Jonsson"; t588.teacherID = "jjn"; t588.birthday = "760920"; teacherlist.Add(t588);
            teacherclass t589 = new teacherclass(); t589.name = "Johanna Jansson"; t589.firstname = "Johanna"; t589.lastname = "Jansson"; t589.teacherID = "jjs"; t589.birthday = "791114"; teacherlist.Add(t589);
            teacherclass t590 = new teacherclass(); t590.name = "Jorma Kallela"; t590.firstname = "Jorma"; t590.lastname = "Kallela"; t590.teacherID = "jka"; t590.birthday = "620629"; teacherlist.Add(t590);
            teacherclass t591 = new teacherclass(); t591.name = "Joyce Kemuma"; t591.firstname = "Joyce"; t591.lastname = "Kemuma"; t591.teacherID = "jke"; t591.birthday = "580325"; teacherlist.Add(t591);
            teacherclass t592 = new teacherclass(); t592.name = "Jofen Kihlström"; t592.firstname = "Jofen"; t592.lastname = "Kihlström"; t592.teacherID = "jki"; t592.birthday = "700522"; teacherlist.Add(t592);
            teacherclass t593 = new teacherclass(); t593.name = "Joakim Lundegård"; t593.firstname = "Joakim"; t593.lastname = "Lundegård"; t593.teacherID = "jkn"; t593.birthday = "541206"; teacherlist.Add(t593);
            teacherclass t594 = new teacherclass(); t594.name = "Johan Kostela"; t594.firstname = "Johan"; t594.lastname = "Kostela"; t594.teacherID = "jko"; t594.birthday = "750106"; teacherlist.Add(t594);
            teacherclass t595 = new teacherclass(); t595.name = "Jaroslaw Kluczniok"; t595.firstname = "Jaroslaw"; t595.lastname = "Kluczniok"; t595.teacherID = "jku"; t595.birthday = "840469"; teacherlist.Add(t595);
            teacherclass t596 = new teacherclass(); t596.name = "Janeth Leksell"; t596.firstname = "Janeth"; t596.lastname = "Leksell"; t596.teacherID = "jle"; t596.birthday = "551107"; teacherlist.Add(t596);
            teacherclass t597 = new teacherclass(); t597.name = "Jan-Erik Lagergren"; t597.firstname = "Jan-Erik"; t597.lastname = "Lagergren"; t597.teacherID = "jlg"; t597.birthday = "440711"; teacherlist.Add(t597);
            teacherclass t598 = new teacherclass(); t598.name = "Jan Wilhelm Lindholm"; t598.firstname = "Jan Wilhelm"; t598.lastname = "Lindholm"; t598.teacherID = "jli"; t598.birthday = "511008"; teacherlist.Add(t598);
            teacherclass t599 = new teacherclass(); t599.name = "Jenny Lönnemyr"; t599.firstname = "Jenny"; t599.lastname = "Lönnemyr"; t599.teacherID = "jlm"; t599.birthday = "770326"; teacherlist.Add(t599);
            teacherclass t600 = new teacherclass(); t600.name = "Jenny Lindkvist"; t600.firstname = "Jenny"; t600.lastname = "Lindkvist"; t600.teacherID = "jln"; t600.birthday = "750414"; teacherlist.Add(t600);
            teacherclass t601 = new teacherclass(); t601.name = "Joacim Larsson Von Garaguly"; t601.firstname = "Joacim"; t601.lastname = "Larsson Von Garaguly"; t601.teacherID = "jlr"; t601.birthday = "730714"; teacherlist.Add(t601);
            teacherclass t602 = new teacherclass(); t602.name = "Johanne Maad"; t602.firstname = "Johanne"; t602.lastname = "Maad"; t602.teacherID = "jmd"; t602.birthday = "701101"; teacherlist.Add(t602);
            teacherclass t603 = new teacherclass(); t603.name = "Juan Manuel Higuerera González"; t603.firstname = "Juan Manuel"; t603.lastname = "Higuerera González"; t603.teacherID = "jmh"; t603.birthday = "741211"; teacherlist.Add(t603);
            teacherclass t604 = new teacherclass(); t604.name = "John Minto-Grover"; t604.firstname = "John"; t604.lastname = "Minto-Grover"; t604.teacherID = "jmi"; t604.birthday = "620111"; teacherlist.Add(t604);
            teacherclass t605 = new teacherclass(); t605.name = "Jessica Wide"; t605.firstname = "Jessica"; t605.lastname = "Wide"; t605.teacherID = "jmj"; t605.birthday = "770419"; teacherlist.Add(t605);
            teacherclass t606 = new teacherclass(); t606.name = "Marianne Liljas Juvas"; t606.firstname = "Marianne"; t606.lastname = "Liljas Juvas"; t606.teacherID = "jml"; t606.birthday = "561210"; teacherlist.Add(t606);
            teacherclass t607 = new teacherclass(); t607.name = "Jan Morawski"; t607.firstname = "Jan"; t607.lastname = "Morawski"; t607.teacherID = "jmo"; t607.birthday = "500914"; teacherlist.Add(t607);
            teacherclass t608 = new teacherclass(); t608.name = "Jonathan M Yachin"; t608.firstname = "Jonathan M"; t608.lastname = "Yachin"; t608.teacherID = "jmy"; t608.birthday = "811128"; teacherlist.Add(t608);
            teacherclass t609 = new teacherclass(); t609.name = "Johan Matz"; t609.firstname = "Johan"; t609.lastname = "Matz"; t609.teacherID = "jmz"; t609.birthday = "690904"; teacherlist.Add(t609);
            teacherclass t610 = new teacherclass(); t610.name = "Judith Narrowe"; t610.firstname = "Judith"; t610.lastname = "Narrowe"; t610.teacherID = "jna"; t610.birthday = "380314"; teacherlist.Add(t610);
            teacherclass t611 = new teacherclass(); t611.name = "John Niendorf"; t611.firstname = "John"; t611.lastname = "Niendorf"; t611.teacherID = "jne"; t611.birthday = "651108"; teacherlist.Add(t611);
            teacherclass t612 = new teacherclass(); t612.name = "Jonas Nilsson"; t612.firstname = "Jonas"; t612.lastname = "Nilsson"; t612.teacherID = "jnl"; t612.birthday = "780715"; teacherlist.Add(t612);
            teacherclass t613 = new teacherclass(); t613.name = "Johan Nordin"; t613.firstname = "Johan"; t613.lastname = "Nordin"; t613.teacherID = "jno"; t613.birthday = "861224"; teacherlist.Add(t613);
            teacherclass t614 = new teacherclass(); t614.name = "Johnny Nilsson"; t614.firstname = "Johnny"; t614.lastname = "Nilsson"; t614.teacherID = "jns"; t614.birthday = "500708"; teacherlist.Add(t614);
            teacherclass t615 = new teacherclass(); t615.name = "Jan Nyren"; t615.firstname = "Jan"; t615.lastname = "Nyren"; t615.teacherID = "jny"; t615.birthday = "410726"; teacherlist.Add(t615);
            teacherclass t616 = new teacherclass(); t616.name = "Jonatan Östberg"; t616.firstname = "Jonatan"; t616.lastname = "Östberg"; t616.teacherID = "job"; t616.birthday = "620310"; teacherlist.Add(t616);
            teacherclass t617 = new teacherclass(); t617.name = "Johanna Edman Olsson"; t617.firstname = "Johanna"; t617.lastname = "Edman Olsson"; t617.teacherID = "joe"; t617.birthday = "750331"; teacherlist.Add(t617);
            teacherclass t618 = new teacherclass(); t618.name = "Jessica Olofsson"; t618.firstname = "Jessica"; t618.lastname = "Olofsson"; t618.teacherID = "jof"; t618.birthday = "740402"; teacherlist.Add(t618);
            teacherclass t619 = new teacherclass(); t619.name = "Josefin Hall"; t619.firstname = "Josefin"; t619.lastname = "Hall"; t619.teacherID = "joh"; t619.birthday = "720625"; teacherlist.Add(t619);
            teacherclass t620 = new teacherclass(); t620.name = "Jonas Stier"; t620.firstname = "Jonas"; t620.lastname = "Stier"; t620.teacherID = "joi"; t620.birthday = "670904"; teacherlist.Add(t620);
            teacherclass t621 = new teacherclass(); t621.name = "Johanna Pettersson"; t621.firstname = "Johanna"; t621.lastname = "Pettersson"; t621.teacherID = "jop"; t621.birthday = "820723"; teacherlist.Add(t621);
            teacherclass t622 = new teacherclass(); t622.name = "Johan Sonne"; t622.firstname = "Johan"; t622.lastname = "Sonne"; t622.teacherID = "jos"; t622.birthday = "740704"; teacherlist.Add(t622);
            teacherclass t623 = new teacherclass(); t623.name = "Jiri Prochazka"; t623.firstname = "Jiri"; t623.lastname = "Prochazka"; t623.teacherID = "jpc"; t623.birthday = "760218"; teacherlist.Add(t623);
            teacherclass t624 = new teacherclass(); t624.name = "Jerry Persson"; t624.firstname = "Jerry"; t624.lastname = "Persson"; t624.teacherID = "jpe"; t624.birthday = "710609"; teacherlist.Add(t624);
            teacherclass t625 = new teacherclass(); t625.name = "Jon Persson"; t625.firstname = "Jon"; t625.lastname = "Persson"; t625.teacherID = "jpn"; t625.birthday = "840525"; teacherlist.Add(t625);
            teacherclass t626 = new teacherclass(); t626.name = "Johanna Ravhed"; t626.firstname = "Johanna"; t626.lastname = "Ravhed"; t626.teacherID = "jra"; t626.birthday = "810714"; teacherlist.Add(t626);
            teacherclass t627 = new teacherclass(); t627.name = "Johanna Rosenblad"; t627.firstname = "Johanna"; t627.lastname = "Rosenblad"; t627.teacherID = "jre"; t627.birthday = "870717"; teacherlist.Add(t627);
            teacherclass t628 = new teacherclass(); t628.name = "Jens Rankvist"; t628.firstname = "Jens"; t628.lastname = "Rankvist"; t628.teacherID = "jrk"; t628.birthday = "851122"; teacherlist.Add(t628);
            teacherclass t629 = new teacherclass(); t629.name = "Jenny Rosén"; t629.firstname = "Jenny"; t629.lastname = "Rosén"; t629.teacherID = "jro"; t629.birthday = "790802"; teacherlist.Add(t629);
            teacherclass t630 = new teacherclass(); t630.name = "Jan Sandberg"; t630.firstname = "Jan"; t630.lastname = "Sandberg"; t630.teacherID = "jsa"; t630.birthday = "620616"; teacherlist.Add(t630);
            teacherclass t631 = new teacherclass(); t631.name = "Johan Söderberg"; t631.firstname = "Johan"; t631.lastname = "Söderberg"; t631.teacherID = "jsd"; t631.birthday = "621114"; teacherlist.Add(t631);
            teacherclass t632 = new teacherclass(); t632.name = "Joacim Svensson"; t632.firstname = "Joacim"; t632.lastname = "Svensson"; t632.teacherID = "jse"; t632.birthday = "850925"; teacherlist.Add(t632);
            teacherclass t633 = new teacherclass(); t633.name = "Julie Skogs"; t633.firstname = "Julie"; t633.lastname = "Skogs"; t633.teacherID = "jsk"; t633.birthday = "580305"; teacherlist.Add(t633);
            teacherclass t634 = new teacherclass(); t634.name = "Jesper Sillanpää"; t634.firstname = "Jesper"; t634.lastname = "Sillanpää"; t634.teacherID = "jsl"; t634.birthday = "851223"; teacherlist.Add(t634);
            teacherclass t635 = new teacherclass(); t635.name = "Jan Simer"; t635.firstname = "Jan"; t635.lastname = "Simer"; t635.teacherID = "jsm"; t635.birthday = "541001"; teacherlist.Add(t635);
            teacherclass t636 = new teacherclass(); t636.name = "Johanna Salomonsson"; t636.firstname = "Johanna"; t636.lastname = "Salomonsson"; t636.teacherID = "jsn"; t636.birthday = "780607"; teacherlist.Add(t636);
            teacherclass t637 = new teacherclass(); t637.name = "Jean-Marie Skoglund"; t637.firstname = "Jean-Marie"; t637.lastname = "Skoglund"; t637.teacherID = "jso"; t637.birthday = "660213"; teacherlist.Add(t637);
            teacherclass t638 = new teacherclass(); t638.name = "Julitta Sundqvist Salinger"; t638.firstname = "Julitta"; t638.lastname = "Sundqvist Salinger"; t638.teacherID = "jss"; t638.birthday = "561211"; teacherlist.Add(t638);
            teacherclass t639 = new teacherclass(); t639.name = "Jan Svärdhagen"; t639.firstname = "Jan"; t639.lastname = "Svärdhagen"; t639.teacherID = "jsv"; t639.birthday = "690116"; teacherlist.Add(t639);
            teacherclass t640 = new teacherclass(); t640.name = "Jennie Tiderman"; t640.firstname = "Jennie"; t640.lastname = "Tiderman"; t640.teacherID = "jti"; t640.birthday = "811117"; teacherlist.Add(t640);
            teacherclass t641 = new teacherclass(); t641.name = "Jonas Tosteby"; t641.firstname = "Jonas"; t641.lastname = "Tosteby"; t641.teacherID = "jto"; t641.birthday = "740329"; teacherlist.Add(t641);
            teacherclass t642 = new teacherclass(); t642.name = "Jenny Turesson"; t642.firstname = "Jenny"; t642.lastname = "Turesson"; t642.teacherID = "jtu"; t642.birthday = "720321"; teacherlist.Add(t642);
            teacherclass t643 = new teacherclass(); t643.name = "Jerker Westin"; t643.firstname = "Jerker"; t643.lastname = "Westin"; t643.teacherID = "jwe"; t643.birthday = "710225"; teacherlist.Add(t643);
            teacherclass t644 = new teacherclass(); t644.name = "Jonathan Russell White"; t644.firstname = "Jonathan Russell"; t644.lastname = "White"; t644.teacherID = "jwh"; t644.birthday = "711213"; teacherlist.Add(t644);
            teacherclass t645 = new teacherclass(); t645.name = "Jennie Vinter"; t645.firstname = "Jennie"; t645.lastname = "Vinter"; t645.teacherID = "jvi"; t645.birthday = "740317"; teacherlist.Add(t645);
            teacherclass t646 = new teacherclass(); t646.name = "Johnny Wingstedt"; t646.firstname = "Johnny"; t646.lastname = "Wingstedt"; t646.teacherID = "jwi"; t646.birthday = "540222"; teacherlist.Add(t646);
            teacherclass t647 = new teacherclass(); t647.name = "Julia Wiman"; t647.firstname = "Julia"; t647.lastname = "Wiman"; t647.teacherID = "jwm"; t647.birthday = "871001"; teacherlist.Add(t647);
            teacherclass t648 = new teacherclass(); t648.name = "Johanna Wennström"; t648.firstname = "Johanna"; t648.lastname = "Wennström"; t648.teacherID = "jwn"; t648.birthday = "831110"; teacherlist.Add(t648);
            teacherclass t649 = new teacherclass(); t649.name = "Jennie Warström"; t649.firstname = "Jennie"; t649.lastname = "Warström"; t649.teacherID = "jwo"; t649.birthday = "770419"; teacherlist.Add(t649);
            teacherclass t650 = new teacherclass(); t650.name = "Jens Westergren"; t650.firstname = "Jens"; t650.lastname = "Westergren"; t650.teacherID = "jws"; t650.birthday = "790515"; teacherlist.Add(t650);
            teacherclass t651 = new teacherclass(); t651.name = "Micheal Kamal Abu-Deeb"; t651.firstname = "Micheal"; t651.lastname = "Kamal Abu-Deeb"; t651.teacherID = "kad"; t651.birthday = "420583"; teacherlist.Add(t651);
            teacherclass t652 = new teacherclass(); t652.name = "Karin Ängeby"; t652.firstname = "Karin"; t652.lastname = "Ängeby"; t652.teacherID = "kag"; t652.birthday = "660801"; teacherlist.Add(t652);
            teacherclass t653 = new teacherclass(); t653.name = "Kerstin Ahlberg"; t653.firstname = "Kerstin"; t653.lastname = "Ahlberg"; t653.teacherID = "kah"; t653.birthday = "560911"; teacherlist.Add(t653);
            teacherclass t654 = new teacherclass(); t654.name = "Konstantin Andreev"; t654.firstname = "Konstantin"; t654.lastname = "Andreev"; t654.teacherID = "kan"; t654.birthday = "790426"; teacherlist.Add(t654);
            teacherclass t655 = new teacherclass(); t655.name = "Karin (Duprod) Johannesson"; t655.firstname = "Karin (Duprod)"; t655.lastname = "Johannesson"; t655.teacherID = "karin.johannesson"; t655.birthday = "541231"; teacherlist.Add(t655);
            teacherclass t656 = new teacherclass(); t656.name = "Kate Avdic"; t656.firstname = "Kate"; t656.lastname = "Avdic"; t656.teacherID = "kav"; t656.birthday = "510507"; teacherlist.Add(t656);
            teacherclass t657 = new teacherclass(); t657.name = "Kent Börjesson"; t657.firstname = "Kent"; t657.lastname = "Börjesson"; t657.teacherID = "kbo"; t657.birthday = "490127"; teacherlist.Add(t657);
            teacherclass t658 = new teacherclass(); t658.name = "Karin Björling"; t658.firstname = "Karin"; t658.lastname = "Björling"; t658.teacherID = "kbr"; t658.birthday = "770429"; teacherlist.Add(t658);
            teacherclass t659 = new teacherclass(); t659.name = "Kumar Babu Surreddi"; t659.firstname = "Kumar Babu"; t659.lastname = "Surreddi"; t659.teacherID = "kbs"; t659.birthday = "770201"; teacherlist.Add(t659);
            teacherclass t660 = new teacherclass(); t660.name = "Kurt Byström"; t660.firstname = "Kurt"; t660.lastname = "Byström"; t660.teacherID = "kby"; t660.birthday = "570910"; teacherlist.Add(t660);
            teacherclass t661 = new teacherclass(); t661.name = "Kenneth Carling"; t661.firstname = "Kenneth"; t661.lastname = "Carling"; t661.teacherID = "kca"; t661.birthday = "671101"; teacherlist.Add(t661);
            teacherclass t662 = new teacherclass(); t662.name = "Kristina Carlsson"; t662.firstname = "Kristina"; t662.lastname = "Carlsson"; t662.teacherID = "kcr"; t662.birthday = "691009"; teacherlist.Add(t662);
            teacherclass t663 = new teacherclass(); t663.name = "Katharina Davidsson"; t663.firstname = "Katharina"; t663.lastname = "Davidsson"; t663.teacherID = "kda"; t663.birthday = "500926"; teacherlist.Add(t663);
            teacherclass t664 = new teacherclass(); t664.name = "Karin Dahlgren"; t664.firstname = "Karin"; t664.lastname = "Dahlgren"; t664.teacherID = "kdh"; t664.birthday = "850805"; teacherlist.Add(t664);
            teacherclass t665 = new teacherclass(); t665.name = "Kalle Dalin"; t665.firstname = "Kalle"; t665.lastname = "Dalin"; t665.teacherID = "kdl"; t665.birthday = "750308"; teacherlist.Add(t665);
            teacherclass t666 = new teacherclass(); t666.name = "Katherina Dodou"; t666.firstname = "Katherina"; t666.lastname = "Dodou"; t666.teacherID = "kdo"; t666.birthday = "800822"; teacherlist.Add(t666);
            teacherclass t667 = new teacherclass(); t667.name = "Kerstin Erlandsson"; t667.firstname = "Kerstin"; t667.lastname = "Erlandsson"; t667.teacherID = "ker"; t667.birthday = "611104"; teacherlist.Add(t667);
            teacherclass t668 = new teacherclass(); t668.name = "Karl-Erik Westergren"; t668.firstname = "Karl-Erik"; t668.lastname = "Westergren"; t668.teacherID = "kew"; t668.birthday = "411024"; teacherlist.Add(t668);
            teacherclass t669 = new teacherclass(); t669.name = "Kim Forsberg"; t669.firstname = "Kim"; t669.lastname = "Forsberg"; t669.teacherID = "kfo"; t669.birthday = "940415"; teacherlist.Add(t669);
            teacherclass t670 = new teacherclass(); t670.name = "Kati Forsberg"; t670.firstname = "Kati"; t670.lastname = "Forsberg"; t670.teacherID = "kfr"; t670.birthday = "771221"; teacherlist.Add(t670);
            teacherclass t671 = new teacherclass(); t671.name = "Katarina Grim"; t671.firstname = "Katarina"; t671.lastname = "Grim"; t671.teacherID = "kgi"; t671.birthday = "710317"; teacherlist.Add(t671);
            teacherclass t672 = new teacherclass(); t672.name = "Kerstin Göras"; t672.firstname = "Kerstin"; t672.lastname = "Göras"; t672.teacherID = "kgo"; t672.birthday = "640524"; teacherlist.Add(t672);
            teacherclass t673 = new teacherclass(); t673.name = "Kerstin Grundelius"; t673.firstname = "Kerstin"; t673.lastname = "Grundelius"; t673.teacherID = "kgr"; t673.birthday = "580225"; teacherlist.Add(t673);
            teacherclass t674 = new teacherclass(); t674.name = "Karl Gummesson"; t674.firstname = "Karl"; t674.lastname = "Gummesson"; t674.teacherID = "kgu"; t674.birthday = "841129"; teacherlist.Add(t674);
            teacherclass t675 = new teacherclass(); t675.name = "Karin Holmlund"; t675.firstname = "Karin"; t675.lastname = "Holmlund"; t675.teacherID = "khl"; t675.birthday = "951107"; teacherlist.Add(t675);
            teacherclass t676 = new teacherclass(); t676.name = "Kerstin Hansson"; t676.firstname = "Kerstin"; t676.lastname = "Hansson"; t676.teacherID = "khn"; t676.birthday = "600302"; teacherlist.Add(t676);
            teacherclass t677 = new teacherclass(); t677.name = "Karl Hansson"; t677.firstname = "Karl"; t677.lastname = "Hansson"; t677.teacherID = "khs"; t677.birthday = "890324"; teacherlist.Add(t677);
            teacherclass t678 = new teacherclass(); t678.name = "Katharina Jacobsson"; t678.firstname = "Katharina"; t678.lastname = "Jacobsson"; t678.teacherID = "kjb"; t678.birthday = "591208"; teacherlist.Add(t678);
            teacherclass t679 = new teacherclass(); t679.name = "Kari Jess"; t679.firstname = "Kari"; t679.lastname = "Jess"; t679.teacherID = "kje"; t679.birthday = "560212"; teacherlist.Add(t679);
            teacherclass t680 = new teacherclass(); t680.name = "Kajsa Klein"; t680.firstname = "Kajsa"; t680.lastname = "Klein"; t680.teacherID = "kkl"; t680.birthday = "731213"; teacherlist.Add(t680);
            teacherclass t681 = new teacherclass(); t681.name = "Kirsti Kuusela"; t681.firstname = "Kirsti"; t681.lastname = "Kuusela"; t681.teacherID = "kku"; t681.birthday = "470225"; teacherlist.Add(t681);
            teacherclass t682 = new teacherclass(); t682.name = "Katarina Lindahl"; t682.firstname = "Katarina"; t682.lastname = "Lindahl"; t682.teacherID = "kla"; t682.birthday = "871124"; teacherlist.Add(t682);
            teacherclass t683 = new teacherclass(); t683.name = "Karin Lundén"; t683.firstname = "Karin"; t683.lastname = "Lundén"; t683.teacherID = "kld"; t683.birthday = "431130"; teacherlist.Add(t683);
            teacherclass t684 = new teacherclass(); t684.name = "Kristina Ledman"; t684.firstname = "Kristina"; t684.lastname = "Ledman"; t684.teacherID = "kle"; t684.birthday = "721106"; teacherlist.Add(t684);
            teacherclass t685 = new teacherclass(); t685.name = "Kristina Lönn"; t685.firstname = "Kristina"; t685.lastname = "Lönn"; t685.teacherID = "kln"; t685.birthday = "640304"; teacherlist.Add(t685);
            teacherclass t686 = new teacherclass(); t686.name = "Klaus Lorenz"; t686.firstname = "Klaus"; t686.lastname = "Lorenz"; t686.teacherID = "klo"; t686.birthday = "550528"; teacherlist.Add(t686);
            teacherclass t687 = new teacherclass(); t687.name = "Kristian Lindström"; t687.firstname = "Kristian"; t687.lastname = "Lindström"; t687.teacherID = "kls"; t687.birthday = "721126"; teacherlist.Add(t687);
            teacherclass t688 = new teacherclass(); t688.name = "Kevin Mckee"; t688.firstname = "Kevin"; t688.lastname = "Mckee"; t688.teacherID = "kmc"; t688.birthday = "610829"; teacherlist.Add(t688);
            teacherclass t689 = new teacherclass(); t689.name = "Kaung Myat Win"; t689.firstname = "Kaung Myat"; t689.lastname = "Win"; t689.teacherID = "kmw"; t689.birthday = "760830"; teacherlist.Add(t689);
            teacherclass t690 = new teacherclass(); t690.name = "Kristina Nordin"; t690.firstname = "Kristina"; t690.lastname = "Nordin"; t690.teacherID = "kno"; t690.birthday = "700626"; teacherlist.Add(t690);
            teacherclass t691 = new teacherclass(); t691.name = "Karin Nordmark"; t691.firstname = "Karin"; t691.lastname = "Nordmark"; t691.teacherID = "knr"; t691.birthday = "740211"; teacherlist.Add(t691);
            teacherclass t692 = new teacherclass(); t692.name = "Kristine Ohrem Andersers"; t692.firstname = "Kristine"; t692.lastname = "Ohrem Andersers"; t692.teacherID = "kob"; t692.birthday = "680424"; teacherlist.Add(t692);
            teacherclass t693 = new teacherclass(); t693.name = "Kerstin Öhrn"; t693.firstname = "Kerstin"; t693.lastname = "Öhrn"; t693.teacherID = "koh"; t693.birthday = "470903"; teacherlist.Add(t693);
            teacherclass t694 = new teacherclass(); t694.name = "Karin Edvardsson"; t694.firstname = "Karin"; t694.lastname = "Edvardsson"; t694.teacherID = "kos"; t694.birthday = "800103"; teacherlist.Add(t694);
            teacherclass t695 = new teacherclass(); t695.name = "Norås Karin Petersen"; t695.firstname = "Norås Karin"; t695.lastname = "Petersen"; t695.teacherID = "kpe"; t695.birthday = "451120"; teacherlist.Add(t695);
            teacherclass t696 = new teacherclass(); t696.name = "Karin Perman"; t696.firstname = "Karin"; t696.lastname = "Perman"; t696.teacherID = "kpm"; t696.birthday = "701001"; teacherlist.Add(t696);
            teacherclass t697 = new teacherclass(); t697.name = "Kajsa Richardsson"; t697.firstname = "Kajsa"; t697.lastname = "Richardsson"; t697.teacherID = "krc"; t697.birthday = "861006"; teacherlist.Add(t697);
            teacherclass t698 = new teacherclass(); t698.name = "Karin Resar"; t698.firstname = "Karin"; t698.lastname = "Resar"; t698.teacherID = "krs"; t698.birthday = "600113"; teacherlist.Add(t698);
            teacherclass t699 = new teacherclass(); t699.name = "Karin Sveland Ludvigsson"; t699.firstname = "Karin"; t699.lastname = "Sveland Ludvigsson"; t699.teacherID = "ksa"; t699.birthday = "760504"; teacherlist.Add(t699);
            teacherclass t700 = new teacherclass(); t700.name = "Kjell Söderlund"; t700.firstname = "Kjell"; t700.lastname = "Söderlund"; t700.teacherID = "ksd"; t700.birthday = "460418"; teacherlist.Add(t700);
            teacherclass t701 = new teacherclass(); t701.name = "Katarina Sundström Rask"; t701.firstname = "Katarina"; t701.lastname = "Sundström Rask"; t701.teacherID = "ksr"; t701.birthday = "801213"; teacherlist.Add(t701);
            teacherclass t702 = new teacherclass(); t702.name = "Kristin Svenson"; t702.firstname = "Kristin"; t702.lastname = "Svenson"; t702.teacherID = "kss"; t702.birthday = "870223"; teacherlist.Add(t702);
            teacherclass t703 = new teacherclass(); t703.name = "Klas Sundberg"; t703.firstname = "Klas"; t703.lastname = "Sundberg"; t703.teacherID = "ksu"; t703.birthday = "670422"; teacherlist.Add(t703);
            teacherclass t704 = new teacherclass(); t704.name = "Kristofer Sidenvall"; t704.firstname = "Kristofer"; t704.lastname = "Sidenvall"; t704.teacherID = "ksv"; t704.birthday = "771208"; teacherlist.Add(t704);
            teacherclass t705 = new teacherclass(); t705.name = "Kerstin Nyhlin Ternulf"; t705.firstname = "Kerstin"; t705.lastname = "Nyhlin Ternulf"; t705.teacherID = "ktn"; t705.birthday = "450630"; teacherlist.Add(t705);
            teacherclass t706 = new teacherclass(); t706.name = "Kristian Vänerhagen"; t706.firstname = "Kristian"; t706.lastname = "Vänerhagen"; t706.teacherID = "kva"; t706.birthday = "790924"; teacherlist.Add(t706);
            teacherclass t707 = new teacherclass(); t707.name = "Karin Wieslander"; t707.firstname = "Karin"; t707.lastname = "Wieslander"; t707.teacherID = "kwi"; t707.birthday = "640727"; teacherlist.Add(t707);
            teacherclass t708 = new teacherclass(); t708.name = "Karl W Sandberg"; t708.firstname = "Karl W"; t708.lastname = "Sandberg"; t708.teacherID = "kws"; t708.birthday = "520530"; teacherlist.Add(t708);
            teacherclass t709 = new teacherclass(); t709.name = "Kimmo Vuori"; t709.firstname = "Kimmo"; t709.lastname = "Vuori"; t709.teacherID = "kvu"; t709.birthday = "740322"; teacherlist.Add(t709);
            teacherclass t710 = new teacherclass(); t710.name = "Karin Yvell"; t710.firstname = "Karin"; t710.lastname = "Yvell"; t710.teacherID = "kyv"; t710.birthday = "630426"; teacherlist.Add(t710);
            teacherclass t711 = new teacherclass(); t711.name = "Lars Åberg"; t711.firstname = "Lars"; t711.lastname = "Åberg"; t711.teacherID = "lab"; t711.birthday = "400905"; teacherlist.Add(t711);
            teacherclass t712 = new teacherclass(); t712.name = "Lars Berge"; t712.firstname = "Lars"; t712.lastname = "Berge"; t712.teacherID = "labe"; t712.birthday = "590309"; teacherlist.Add(t712);
            teacherclass t713 = new teacherclass(); t713.name = "Lars-Åke Glans"; t713.firstname = "Lars-Åke"; t713.lastname = "Glans"; t713.teacherID = "lag"; t713.birthday = "440120"; teacherlist.Add(t713);
            teacherclass t714 = new teacherclass(); t714.name = "Lena Åhman"; t714.firstname = "Lena"; t714.lastname = "Åhman"; t714.teacherID = "lah"; t714.birthday = "530306"; teacherlist.Add(t714);
            teacherclass t715 = new teacherclass(); t715.name = "Leif Åkerblom"; t715.firstname = "Leif"; t715.lastname = "Åkerblom"; t715.teacherID = "lak"; t715.birthday = "510604"; teacherlist.Add(t715);
            teacherclass t716 = new teacherclass(); t716.name = "Liselott Åkerblom"; t716.firstname = "Liselott"; t716.lastname = "Åkerblom"; t716.teacherID = "lam"; t716.birthday = "851004"; teacherlist.Add(t716);
            teacherclass t717 = new teacherclass(); t717.name = "Lennart Andersson"; t717.firstname = "Lennart"; t717.lastname = "Andersson"; t717.teacherID = "lan"; t717.birthday = "370114"; teacherlist.Add(t717);
            teacherclass t718 = new teacherclass(); t718.name = "Lars-Åke Östlin"; t718.firstname = "Lars-Åke"; t718.lastname = "Östlin"; t718.teacherID = "lao"; t718.birthday = "680304"; teacherlist.Add(t718);
            teacherclass t719 = new teacherclass(); t719.name = "Lena Arnesdotter"; t719.firstname = "Lena"; t719.lastname = "Arnesdotter"; t719.teacherID = "lar"; t719.birthday = "540205"; teacherlist.Add(t719);
            teacherclass t720 = new teacherclass(); t720.name = "Lena Åström"; t720.firstname = "Lena"; t720.lastname = "Åström"; t720.teacherID = "las"; t720.birthday = "650908"; teacherlist.Add(t720);
            teacherclass t721 = new teacherclass(); t721.name = "Lars-Erik Alkvist"; t721.firstname = "Lars-Erik"; t721.lastname = "Alkvist"; t721.teacherID = "lav"; t721.birthday = "540401"; teacherlist.Add(t721);
            teacherclass t722 = new teacherclass(); t722.name = "Lars Båtefalk"; t722.firstname = "Lars"; t722.lastname = "Båtefalk"; t722.teacherID = "lba"; t722.birthday = "660430"; teacherlist.Add(t722);
            teacherclass t723 = new teacherclass(); t723.name = "Lena Eriksson Back"; t723.firstname = "Lena"; t723.lastname = "Eriksson Back"; t723.teacherID = "lbc"; t723.birthday = "680130"; teacherlist.Add(t723);
            teacherclass t724 = new teacherclass(); t724.name = "Lovisa Berg"; t724.firstname = "Lovisa"; t724.lastname = "Berg"; t724.teacherID = "lbg"; t724.birthday = "840117"; teacherlist.Add(t724);
            teacherclass t725 = new teacherclass(); t725.name = "Lena Bjerhammar"; t725.firstname = "Lena"; t725.lastname = "Bjerhammar"; t725.teacherID = "lbj"; t725.birthday = "530407"; teacherlist.Add(t725);
            teacherclass t726 = new teacherclass(); t726.name = "Lena Bergkvist"; t726.firstname = "Lena"; t726.lastname = "Bergkvist"; t726.teacherID = "lbk"; t726.birthday = "551107"; teacherlist.Add(t726);
            teacherclass t727 = new teacherclass(); t727.name = "Lennart Blomqvist"; t727.firstname = "Lennart"; t727.lastname = "Blomqvist"; t727.teacherID = "lbl"; t727.birthday = "650401"; teacherlist.Add(t727);
            teacherclass t728 = new teacherclass(); t728.name = "Leif Borgert"; t728.firstname = "Leif"; t728.lastname = "Borgert"; t728.teacherID = "lbo"; t728.birthday = "430902"; teacherlist.Add(t728);
            teacherclass t729 = new teacherclass(); t729.name = "Linda Berghov"; t729.firstname = "Linda"; t729.lastname = "Berghov"; t729.teacherID = "lbv"; t729.birthday = "760930"; teacherlist.Add(t729);
            teacherclass t730 = new teacherclass(); t730.name = "Luis Conde-Costas"; t730.firstname = "Luis"; t730.lastname = "Conde-Costas"; t730.teacherID = "lcc"; t730.birthday = "600226"; teacherlist.Add(t730);
            teacherclass t731 = new teacherclass(); t731.name = "Lena Dahlberg"; t731.firstname = "Lena"; t731.lastname = "Dahlberg"; t731.teacherID = "ldh"; t731.birthday = "701014"; teacherlist.Add(t731);
            teacherclass t732 = new teacherclass(); t732.name = "Lena Dahlstrand"; t732.firstname = "Lena"; t732.lastname = "Dahlstrand"; t732.teacherID = "ldl"; t732.birthday = "690201"; teacherlist.Add(t732);
            teacherclass t733 = new teacherclass(); t733.name = "Lena Birath"; t733.firstname = "Lena"; t733.lastname = "Birath"; t733.teacherID = "leb"; t733.birthday = "681211"; teacherlist.Add(t733);
            teacherclass t734 = new teacherclass(); t734.name = "Lennart Berg"; t734.firstname = "Lennart"; t734.lastname = "Berg"; t734.teacherID = "lebe"; t734.birthday = "700217"; teacherlist.Add(t734);
            teacherclass t735 = new teacherclass(); t735.name = "Lars-Erik Cederlöf"; t735.firstname = "Lars-Erik"; t735.lastname = "Cederlöf"; t735.teacherID = "lec"; t735.birthday = "470627"; teacherlist.Add(t735);
            teacherclass t736 = new teacherclass(); t736.name = "Lee Nordevald-Sjöberg"; t736.firstname = "Lee"; t736.lastname = "Nordevald-Sjöberg"; t736.teacherID = "lee"; t736.birthday = "530616"; teacherlist.Add(t736);
            teacherclass t737 = new teacherclass(); t737.name = "Linda Eklund"; t737.firstname = "Linda"; t737.lastname = "Eklund"; t737.teacherID = "lek"; t737.birthday = "791101"; teacherlist.Add(t737);
            teacherclass t738 = new teacherclass(); t738.name = "Lars-Erik Lindgren"; t738.firstname = "Lars-Erik"; t738.lastname = "Lindgren"; t738.teacherID = "lel"; t738.birthday = "560511"; teacherlist.Add(t738);
            teacherclass t739 = new teacherclass(); t739.name = "Louise Enström"; t739.firstname = "Louise"; t739.lastname = "Enström"; t739.teacherID = "len"; t739.birthday = "690322"; teacherlist.Add(t739);
            teacherclass t740 = new teacherclass(); t740.name = "Lottie Erdman-Sundh"; t740.firstname = "Lottie"; t740.lastname = "Erdman-Sundh"; t740.teacherID = "les"; t740.birthday = "510329"; teacherlist.Add(t740);
            teacherclass t741 = new teacherclass(); t741.name = "Lennart Ewenson"; t741.firstname = "Lennart"; t741.lastname = "Ewenson"; t741.teacherID = "lew"; t741.birthday = "510121"; teacherlist.Add(t741);
            teacherclass t742 = new teacherclass(); t742.name = "Louise Fredriksson"; t742.firstname = "Louise"; t742.lastname = "Fredriksson"; t742.teacherID = "lfd"; t742.birthday = "870831"; teacherlist.Add(t742);
            teacherclass t743 = new teacherclass(); t743.name = "Liselotte Frisk"; t743.firstname = "Liselotte"; t743.lastname = "Frisk"; t743.teacherID = "lfi"; t743.birthday = "590311"; teacherlist.Add(t743);
            teacherclass t744 = new teacherclass(); t744.name = "Lisa Fredriksson"; t744.firstname = "Lisa"; t744.lastname = "Fredriksson"; t744.teacherID = "lfk"; t744.birthday = "860310"; teacherlist.Add(t744);
            teacherclass t745 = new teacherclass(); t745.name = "Lovisa Furingsten"; t745.firstname = "Lovisa"; t745.lastname = "Furingsten"; t745.teacherID = "lfu"; t745.birthday = "740420"; teacherlist.Add(t745);
            teacherclass t746 = new teacherclass(); t746.name = "Liza Greiz"; t746.firstname = "Liza"; t746.lastname = "Greiz"; t746.teacherID = "lgr"; t746.birthday = "790920"; teacherlist.Add(t746);
            teacherclass t747 = new teacherclass(); t747.name = "Linnea Gustafsson"; t747.firstname = "Linnea"; t747.lastname = "Gustafsson"; t747.teacherID = "lgt"; t747.birthday = "860220"; teacherlist.Add(t747);
            teacherclass t748 = new teacherclass(); t748.name = "Lisa Hermansson Sens"; t748.firstname = "Lisa"; t748.lastname = "Hermansson Sens"; t748.teacherID = "lhs"; t748.birthday = "870615"; teacherlist.Add(t748);
            teacherclass t749 = new teacherclass(); t749.name = "Linda Bergnér"; t749.firstname = "Linda"; t749.lastname = "Bergnér"; t749.teacherID = "libe"; t749.birthday = "800206"; teacherlist.Add(t749);
            teacherclass t750 = new teacherclass(); t750.name = "Lisa Ek (Jonason)"; t750.firstname = "Lisa"; t750.lastname = "Ek (Jonason)"; t750.teacherID = "ljo"; t750.birthday = "860103"; teacherlist.Add(t750);
            teacherclass t751 = new teacherclass(); t751.name = "Lenka Klimplova"; t751.firstname = "Lenka"; t751.lastname = "Klimplova"; t751.teacherID = "lki"; t751.birthday = "791227"; teacherlist.Add(t751);
            teacherclass t752 = new teacherclass(); t752.name = "Louise Vestlie"; t752.firstname = "Louise"; t752.lastname = "Vestlie"; t752.teacherID = "lkl"; t752.birthday = "590124"; teacherlist.Add(t752);
            teacherclass t753 = new teacherclass(); t753.name = "Lars Krantz"; t753.firstname = "Lars"; t753.lastname = "Krantz"; t753.teacherID = "lkn"; t753.birthday = "600810"; teacherlist.Add(t753);
            teacherclass t754 = new teacherclass(); t754.name = "Elisabeth Karlströms-Rosell"; t754.firstname = "Elisabeth"; t754.lastname = "Karlströms-Rosell"; t754.teacherID = "lkr"; t754.birthday = "540218"; teacherlist.Add(t754);
            teacherclass t755 = new teacherclass(); t755.name = "Lars Linder"; t755.firstname = "Lars"; t755.lastname = "Linder"; t755.teacherID = "lld"; t755.birthday = "610615"; teacherlist.Add(t755);
            teacherclass t756 = new teacherclass(); t756.name = "Lars Löfquist"; t756.firstname = "Lars"; t756.lastname = "Löfquist"; t756.teacherID = "llf"; t756.birthday = "751111"; teacherlist.Add(t756);
            teacherclass t757 = new teacherclass(); t757.name = "Hu Lung-Lung"; t757.firstname = "Hu"; t757.lastname = "Lung-Lung"; t757.teacherID = "llh"; t757.birthday = "760729"; teacherlist.Add(t757);
            teacherclass t758 = new teacherclass(); t758.name = "Ann-Charlotte Lindén"; t758.firstname = "Ann-Charlotte"; t758.lastname = "Lindén"; t758.teacherID = "lli"; t758.birthday = "680601"; teacherlist.Add(t758);
            teacherclass t759 = new teacherclass(); t759.name = "Lottie Lofors-Nyblom"; t759.firstname = "Lottie"; t759.lastname = "Lofors-Nyblom"; t759.teacherID = "llo"; t759.birthday = "500131"; teacherlist.Add(t759);
            teacherclass t760 = new teacherclass(); t760.name = "Linde Lindqvist"; t760.firstname = "Linde"; t760.lastname = "Lindqvist"; t760.teacherID = "llv"; t760.birthday = "850411"; teacherlist.Add(t760);
            teacherclass t761 = new teacherclass(); t761.name = "Lena Marmstål Hammar"; t761.firstname = "Lena"; t761.lastname = "Marmstål Hammar"; t761.teacherID = "lma"; t761.birthday = "790705"; teacherlist.Add(t761);
            teacherclass t762 = new teacherclass(); t762.name = "Lena Menkens"; t762.firstname = "Lena"; t762.lastname = "Menkens"; t762.teacherID = "lme"; t762.birthday = "580529"; teacherlist.Add(t762);
            teacherclass t763 = new teacherclass(); t763.name = "Lena-Maria Busk"; t763.firstname = "Lena-Maria"; t763.lastname = "Busk"; t763.teacherID = "lmh"; t763.birthday = "551231"; teacherlist.Add(t763);
            teacherclass t764 = new teacherclass(); t764.name = "Lisa Missing"; t764.firstname = "Lisa"; t764.lastname = "Missing"; t764.teacherID = "lmi"; t764.birthday = "731210"; teacherlist.Add(t764);
            teacherclass t765 = new teacherclass(); t765.name = "Lars Karlsson"; t765.firstname = "Lars"; t765.lastname = "Karlsson"; t765.teacherID = "lmk"; t765.birthday = "530315"; teacherlist.Add(t765);
            teacherclass t766 = new teacherclass(); t766.name = "Lina Mörk"; t766.firstname = "Lina"; t766.lastname = "Mörk"; t766.teacherID = "lmo"; t766.birthday = "780517"; teacherlist.Add(t766);
            teacherclass t767 = new teacherclass(); t767.name = "Lisa Myrzell"; t767.firstname = "Lisa"; t767.lastname = "Myrzell"; t767.teacherID = "lmy"; t767.birthday = "571003"; teacherlist.Add(t767);
            teacherclass t768 = new teacherclass(); t768.name = "Lena Olai"; t768.firstname = "Lena"; t768.lastname = "Olai"; t768.teacherID = "loa"; t768.birthday = "580522"; teacherlist.Add(t768);
            teacherclass t769 = new teacherclass(); t769.name = "Lasse Olsson"; t769.firstname = "Lasse"; t769.lastname = "Olsson"; t769.teacherID = "loo"; t769.birthday = "441116"; teacherlist.Add(t769);
            teacherclass t770 = new teacherclass(); t770.name = "Lars Petterson"; t770.firstname = "Lars"; t770.lastname = "Petterson"; t770.teacherID = "lpe"; t770.birthday = "480117"; teacherlist.Add(t770);
            teacherclass t771 = new teacherclass(); t771.name = "Lena Perrault"; t771.firstname = "Lena"; t771.lastname = "Perrault"; t771.teacherID = "lpr"; t771.birthday = "600628"; teacherlist.Add(t771);
            teacherclass t772 = new teacherclass(); t772.name = "Lena Pettersson"; t772.firstname = "Lena"; t772.lastname = "Pettersson"; t772.teacherID = "lpt"; t772.birthday = "601003"; teacherlist.Add(t772);
            teacherclass t773 = new teacherclass(); t773.name = "Loretta Qwarnström"; t773.firstname = "Loretta"; t773.lastname = "Qwarnström"; t773.teacherID = "lqw"; t773.birthday = "570822"; teacherlist.Add(t773);
            teacherclass t774 = new teacherclass(); t774.name = "Liivi Jakobson"; t774.firstname = "Liivi"; t774.lastname = "Jakobson"; t774.teacherID = "lra"; t774.birthday = "621211"; teacherlist.Add(t774);
            teacherclass t775 = new teacherclass(); t775.name = "Lars Rönnegård"; t775.firstname = "Lars"; t775.lastname = "Rönnegård"; t775.teacherID = "lrn"; t775.birthday = "700323"; teacherlist.Add(t775);
            teacherclass t776 = new teacherclass(); t776.name = "Linda Sjöberg"; t776.firstname = "Linda"; t776.lastname = "Sjöberg"; t776.teacherID = "lsj"; t776.birthday = "790411"; teacherlist.Add(t776);
            teacherclass t777 = new teacherclass(); t777.name = "Lena Skoglund"; t777.firstname = "Lena"; t777.lastname = "Skoglund"; t777.teacherID = "lsk"; t777.birthday = "800212"; teacherlist.Add(t777);
            teacherclass t778 = new teacherclass(); t778.name = "Lovisa Sumpter"; t778.firstname = "Lovisa"; t778.lastname = "Sumpter"; t778.teacherID = "lsm"; t778.birthday = "740417"; teacherlist.Add(t778);
            teacherclass t779 = new teacherclass(); t779.name = "Linda Synnermo"; t779.firstname = "Linda"; t779.lastname = "Synnermo"; t779.teacherID = "lsy"; t779.birthday = "740222"; teacherlist.Add(t779);
            teacherclass t780 = new teacherclass(); t780.name = "Lena Tigerstrand"; t780.firstname = "Lena"; t780.lastname = "Tigerstrand"; t780.teacherID = "lti"; t780.birthday = "500203"; teacherlist.Add(t780);
            teacherclass t781 = new teacherclass(); t781.name = "Lars Thorin"; t781.firstname = "Lars"; t781.lastname = "Thorin"; t781.teacherID = "lto"; t781.birthday = "700430"; teacherlist.Add(t781);
            teacherclass t782 = new teacherclass(); t782.name = "Lars Troive"; t782.firstname = "Lars"; t782.lastname = "Troive"; t782.teacherID = "ltr"; t782.birthday = "620502"; teacherlist.Add(t782);
            teacherclass t783 = new teacherclass(); t783.name = "Louise Trygg"; t783.firstname = "Louise"; t783.lastname = "Trygg"; t783.teacherID = "lty"; t783.birthday = "660907"; teacherlist.Add(t783);
            teacherclass t784 = new teacherclass(); t784.name = "Linda Uddenfeldt"; t784.firstname = "Linda"; t784.lastname = "Uddenfeldt"; t784.teacherID = "lud"; t784.birthday = "760610"; teacherlist.Add(t784);
            teacherclass t785 = new teacherclass(); t785.name = "Lars Wallin"; t785.firstname = "Lars"; t785.lastname = "Wallin"; t785.teacherID = "lwa"; t785.birthday = "550726"; teacherlist.Add(t785);
            teacherclass t786 = new teacherclass(); t786.name = "Liselotte Åström"; t786.firstname = "Liselotte"; t786.lastname = "Åström"; t786.teacherID = "lvb"; t786.birthday = "540901"; teacherlist.Add(t786);
            teacherclass t787 = new teacherclass(); t787.name = "Lena Wedberg"; t787.firstname = "Lena"; t787.lastname = "Wedberg"; t787.teacherID = "lwb"; t787.birthday = "560722"; teacherlist.Add(t787);
            teacherclass t788 = new teacherclass(); t788.name = "Lotta Wedman"; t788.firstname = "Lotta"; t788.lastname = "Wedman"; t788.teacherID = "lwd"; t788.birthday = "750427"; teacherlist.Add(t788);
            teacherclass t789 = new teacherclass(); t789.name = "Lillemor Vallin Eckardt"; t789.firstname = "Lillemor"; t789.lastname = "Vallin Eckardt"; t789.teacherID = "lve"; t789.birthday = "650809"; teacherlist.Add(t789);
            teacherclass t790 = new teacherclass(); t790.name = "Lennart Westman"; t790.firstname = "Lennart"; t790.lastname = "Westman"; t790.teacherID = "lwe"; t790.birthday = "511230"; teacherlist.Add(t790);
            teacherclass t791 = new teacherclass(); t791.name = "Lena Von Garaguly"; t791.firstname = "Lena Von"; t791.lastname = "Garaguly"; t791.teacherID = "lvg"; t791.birthday = "461217"; teacherlist.Add(t791);
            teacherclass t792 = new teacherclass(); t792.name = "Lars Wedholm"; t792.firstname = "Lars"; t792.lastname = "Wedholm"; t792.teacherID = "lwh"; t792.birthday = "730808"; teacherlist.Add(t792);
            teacherclass t793 = new teacherclass(); t793.name = "Linda Vixner"; t793.firstname = "Linda"; t793.lastname = "Vixner"; t793.teacherID = "lvi"; t793.birthday = "720321"; teacherlist.Add(t793);
            teacherclass t794 = new teacherclass(); t794.name = "Lena Wilhelmson"; t794.firstname = "Lena"; t794.lastname = "Wilhelmson"; t794.teacherID = "lwi"; t794.birthday = "520724"; teacherlist.Add(t794);
            teacherclass t795 = new teacherclass(); t795.name = "Lisa Westlund"; t795.firstname = "Lisa"; t795.lastname = "Westlund"; t795.teacherID = "lws"; t795.birthday = "850213"; teacherlist.Add(t795);
            teacherclass t796 = new teacherclass(); t796.name = "Elisabeth Eliasson"; t796.firstname = "Elisabeth"; t796.lastname = "Eliasson"; t796.teacherID = "lwu"; t796.birthday = "690404"; teacherlist.Add(t796);
            teacherclass t797 = new teacherclass(); t797.name = "Louise Yngvesson"; t797.firstname = "Louise"; t797.lastname = "Yngvesson"; t797.teacherID = "lyn"; t797.birthday = "820412"; teacherlist.Add(t797);
            teacherclass t798 = new teacherclass(); t798.name = "Md Moudud Alam"; t798.firstname = "Md Moudud"; t798.lastname = "Alam"; t798.teacherID = "maa"; t798.birthday = "760101"; teacherlist.Add(t798);
            teacherclass t799 = new teacherclass(); t799.name = "Marita Andersson"; t799.firstname = "Marita"; t799.lastname = "Andersson"; t799.teacherID = "maad"; t799.birthday = "610508"; teacherlist.Add(t799);
            teacherclass t800 = new teacherclass(); t800.name = "Mathias Andersson"; t800.firstname = "Mathias"; t800.lastname = "Andersson"; t800.teacherID = "maae"; t800.birthday = "740530"; teacherlist.Add(t800);
            teacherclass t801 = new teacherclass(); t801.name = "Martina Algotsson"; t801.firstname = "Martina"; t801.lastname = "Algotsson"; t801.teacherID = "maal"; t801.birthday = "750417"; teacherlist.Add(t801);
            teacherclass t802 = new teacherclass(); t802.name = "Mats Anderson"; t802.firstname = "Mats"; t802.lastname = "Anderson"; t802.teacherID = "maan"; t802.birthday = "590616"; teacherlist.Add(t802);
            teacherclass t803 = new teacherclass(); t803.name = "Martin Andersen"; t803.firstname = "Martin"; t803.lastname = "Andersen"; t803.teacherID = "maar"; t803.birthday = "850325"; teacherlist.Add(t803);
            teacherclass t804 = new teacherclass(); t804.name = "Mandy Bengts"; t804.firstname = "Mandy"; t804.lastname = "Bengts"; t804.teacherID = "mab"; t804.birthday = "700610"; teacherlist.Add(t804);
            teacherclass t805 = new teacherclass(); t805.name = "Mats Braun"; t805.firstname = "Mats"; t805.lastname = "Braun"; t805.teacherID = "maba"; t805.birthday = "760411"; teacherlist.Add(t805);
            teacherclass t806 = new teacherclass(); t806.name = "Martin Bergdahl"; t806.firstname = "Martin"; t806.lastname = "Bergdahl"; t806.teacherID = "mabe"; t806.birthday = "470523"; teacherlist.Add(t806);
            teacherclass t807 = new teacherclass(); t807.name = "Mari Hysing"; t807.firstname = "Mari"; t807.lastname = "Hysing"; t807.teacherID = "mabg"; t807.birthday = "681204"; teacherlist.Add(t807);
            teacherclass t808 = new teacherclass(); t808.name = "Magnus Björkman"; t808.firstname = "Magnus"; t808.lastname = "Björkman"; t808.teacherID = "mabj"; t808.birthday = "691021"; teacherlist.Add(t808);
            teacherclass t809 = new teacherclass(); t809.name = "Maryam Barkadehi"; t809.firstname = "Maryam"; t809.lastname = "Barkadehi"; t809.teacherID = "mabk"; t809.birthday = "650729"; teacherlist.Add(t809);
            teacherclass t810 = new teacherclass(); t810.name = "Marcus Berglund"; t810.firstname = "Marcus"; t810.lastname = "Berglund"; t810.teacherID = "mabl"; t810.birthday = "800115"; teacherlist.Add(t810);
            teacherclass t811 = new teacherclass(); t811.name = "Maryam Bourbour"; t811.firstname = "Maryam"; t811.lastname = "Bourbour"; t811.teacherID = "mabo"; t811.birthday = "770911"; teacherlist.Add(t811);
            teacherclass t812 = new teacherclass(); t812.name = "Matilda Buske"; t812.firstname = "Matilda"; t812.lastname = "Buske"; t812.teacherID = "mabu"; t812.birthday = "791206"; teacherlist.Add(t812);
            teacherclass t813 = new teacherclass(); t813.name = "Maria Engström"; t813.firstname = "Maria"; t813.lastname = "Engström"; t813.teacherID = "mae"; t813.birthday = "680908"; teacherlist.Add(t813);
            teacherclass t814 = new teacherclass(); t814.name = "Magdalena Santana"; t814.firstname = "Magdalena"; t814.lastname = "Santana"; t814.teacherID = "mags"; t814.birthday = "780620"; teacherlist.Add(t814);
            teacherclass t815 = new teacherclass(); t815.name = "Ali Joudi"; t815.firstname = "Ali"; t815.lastname = "Joudi"; t815.teacherID = "maj"; t815.birthday = "800124"; teacherlist.Add(t815);
            teacherclass t816 = new teacherclass(); t816.name = "Marie-Louise Jakobsson"; t816.firstname = "Marie-Louise"; t816.lastname = "Jakobsson"; t816.teacherID = "maja"; t816.birthday = "670624"; teacherlist.Add(t816);
            teacherclass t817 = new teacherclass(); t817.name = "Madeleine Jansson"; t817.firstname = "Madeleine"; t817.lastname = "Jansson"; t817.teacherID = "majs"; t817.birthday = "910326"; teacherlist.Add(t817);
            teacherclass t818 = new teacherclass(); t818.name = "Martin Karlsson"; t818.firstname = "Martin"; t818.lastname = "Karlsson"; t818.teacherID = "maka"; t818.birthday = "820715"; teacherlist.Add(t818);
            teacherclass t819 = new teacherclass(); t819.name = "Margareta Berglund"; t819.firstname = "Margareta"; t819.lastname = "Berglund"; t819.teacherID = "mal"; t819.birthday = "530714"; teacherlist.Add(t819);
            teacherclass t820 = new teacherclass(); t820.name = "Marie Linder"; t820.firstname = "Marie"; t820.lastname = "Linder"; t820.teacherID = "mali"; t820.birthday = "630119"; teacherlist.Add(t820);
            teacherclass t821 = new teacherclass(); t821.name = "Maria Andersén"; t821.firstname = "Maria"; t821.lastname = "Andersén"; t821.teacherID = "man"; t821.birthday = "820504"; teacherlist.Add(t821);
            teacherclass t822 = new teacherclass(); t822.name = "Magnus Nilsson"; t822.firstname = "Magnus"; t822.lastname = "Nilsson"; t822.teacherID = "manl"; t822.birthday = "660625"; teacherlist.Add(t822);
            teacherclass t823 = new teacherclass(); t823.name = "Man Gao"; t823.firstname = "Man"; t823.lastname = "Gao"; t823.teacherID = "mao"; t823.birthday = "771113"; teacherlist.Add(t823);
            teacherclass t824 = new teacherclass(); t824.name = "Mats Öhlén"; t824.firstname = "Mats"; t824.lastname = "Öhlén"; t824.teacherID = "maoh"; t824.birthday = "770327"; teacherlist.Add(t824);
            teacherclass t825 = new teacherclass(); t825.name = "Maria Olson"; t825.firstname = "Maria"; t825.lastname = "Olson"; t825.teacherID = "maol"; t825.birthday = "690514"; teacherlist.Add(t825);
            teacherclass t826 = new teacherclass(); t826.name = "Maria Petersson"; t826.firstname = "Maria"; t826.lastname = "Petersson"; t826.teacherID = "map"; t826.birthday = "560709"; teacherlist.Add(t826);
            teacherclass t827 = new teacherclass(); t827.name = "Mattias Aronsson"; t827.firstname = "Mattias"; t827.lastname = "Aronsson"; t827.teacherID = "mar"; t827.birthday = "710110"; teacherlist.Add(t827);
            teacherclass t828 = new teacherclass(); t828.name = "Maria Sätterberg"; t828.firstname = "Maria"; t828.lastname = "Sätterberg"; t828.teacherID = "masa"; t828.birthday = "711230"; teacherlist.Add(t828);
            teacherclass t829 = new teacherclass(); t829.name = "Maria Särnblad"; t829.firstname = "Maria"; t829.lastname = "Särnblad"; t829.teacherID = "masn"; t829.birthday = "640107"; teacherlist.Add(t829);
            teacherclass t830 = new teacherclass(); t830.name = "Mathias Strandberg"; t830.firstname = "Mathias"; t830.lastname = "Strandberg"; t830.teacherID = "masr"; t830.birthday = "841207"; teacherlist.Add(t830);
            teacherclass t831 = new teacherclass(); t831.name = "Mats Landström"; t831.firstname = "Mats"; t831.lastname = "Landström"; t831.teacherID = "mat"; t831.birthday = "670922"; teacherlist.Add(t831);
            teacherclass t832 = new teacherclass(); t832.name = "Mauro Schiavella"; t832.firstname = "Mauro"; t832.lastname = "Schiavella"; t832.teacherID = "mav"; t832.birthday = "730574"; teacherlist.Add(t832);
            teacherclass t833 = new teacherclass(); t833.name = "Märet Brunnstedt"; t833.firstname = "Märet"; t833.lastname = "Brunnstedt"; t833.teacherID = "mba"; t833.birthday = "610209"; teacherlist.Add(t833);
            teacherclass t834 = new teacherclass(); t834.name = "Mats Barrdahl"; t834.firstname = "Mats"; t834.lastname = "Barrdahl"; t834.teacherID = "mbd"; t834.birthday = "490420"; teacherlist.Add(t834);
            teacherclass t835 = new teacherclass(); t835.name = "Magnus Berglund"; t835.firstname = "Magnus"; t835.lastname = "Berglund"; t835.teacherID = "mbe"; t835.birthday = "661210"; teacherlist.Add(t835);
            teacherclass t836 = new teacherclass(); t836.name = "Mikael Berg"; t836.firstname = "Mikael"; t836.lastname = "Berg"; t836.teacherID = "mbg"; t836.birthday = "700525"; teacherlist.Add(t836);
            teacherclass t837 = new teacherclass(); t837.name = "Maria Bjerneby Häll"; t837.firstname = "Maria"; t837.lastname = "Bjerneby Häll"; t837.teacherID = "mbh"; t837.birthday = "500728"; teacherlist.Add(t837);
            teacherclass t838 = new teacherclass(); t838.name = "Maria Boström"; t838.firstname = "Maria"; t838.lastname = "Boström"; t838.teacherID = "mbm"; t838.birthday = "681108"; teacherlist.Add(t838);
            teacherclass t839 = new teacherclass(); t839.name = "Magnus Bohlin"; t839.firstname = "Magnus"; t839.lastname = "Bohlin"; t839.teacherID = "mbo"; t839.birthday = "500101"; teacherlist.Add(t839);
            teacherclass t840 = new teacherclass(); t840.name = "Magnus Carlsson"; t840.firstname = "Magnus"; t840.lastname = "Carlsson"; t840.teacherID = "mca"; t840.birthday = "741228"; teacherlist.Add(t840);
            teacherclass t841 = new teacherclass(); t841.name = "Maria Cederblad"; t841.firstname = "Maria"; t841.lastname = "Cederblad"; t841.teacherID = "mce"; t841.birthday = "680102"; teacherlist.Add(t841);
            teacherclass t842 = new teacherclass(); t842.name = "Mari-Cristin Malm"; t842.firstname = "Mari-Cristin"; t842.lastname = "Malm"; t842.teacherID = "mcm"; t842.birthday = "560916"; teacherlist.Add(t842);
            teacherclass t843 = new teacherclass(); t843.name = "Megan Case"; t843.firstname = "Megan"; t843.lastname = "Case"; t843.teacherID = "mcs"; t843.birthday = "760717"; teacherlist.Add(t843);
            teacherclass t844 = new teacherclass(); t844.name = "Mattias Dahlberg"; t844.firstname = "Mattias"; t844.lastname = "Dahlberg"; t844.teacherID = "mda"; t844.birthday = "710219"; teacherlist.Add(t844);
            teacherclass t845 = new teacherclass(); t845.name = "Maria Deldén"; t845.firstname = "Maria"; t845.lastname = "Deldén"; t845.teacherID = "mde"; t845.birthday = "630627"; teacherlist.Add(t845);
            teacherclass t846 = new teacherclass(); t846.name = "David Molnár"; t846.firstname = "David"; t846.lastname = "Molnár"; t846.teacherID = "mdi"; t846.birthday = "900926"; teacherlist.Add(t846);
            teacherclass t847 = new teacherclass(); t847.name = "Mark Dougherty"; t847.firstname = "Mark"; t847.lastname = "Dougherty"; t847.teacherID = "mdo"; t847.birthday = "670320"; teacherlist.Add(t847);
            teacherclass t848 = new teacherclass(); t848.name = "Mengjie Han"; t848.firstname = "Mengjie"; t848.lastname = "Han"; t848.teacherID = "mea"; t848.birthday = "851123"; teacherlist.Add(t848);
            teacherclass t849 = new teacherclass(); t849.name = "Maren Eckart (Jönsson)"; t849.firstname = "Maren"; t849.lastname = "Eckart (Jönsson)"; t849.teacherID = "mec"; t849.birthday = "650124"; teacherlist.Add(t849);
            teacherclass t850 = new teacherclass(); t850.name = "Marie Edqvist"; t850.firstname = "Marie"; t850.lastname = "Edqvist"; t850.teacherID = "med"; t850.birthday = "641024"; teacherlist.Add(t850);
            teacherclass t851 = new teacherclass(); t851.name = "Mona Engberg"; t851.firstname = "Mona"; t851.lastname = "Engberg"; t851.teacherID = "meg"; t851.birthday = "371204"; teacherlist.Add(t851);
            teacherclass t852 = new teacherclass(); t852.name = "Marie Elf"; t852.firstname = "Marie"; t852.lastname = "Elf"; t852.teacherID = "mel"; t852.birthday = "621001"; teacherlist.Add(t852);
            teacherclass t853 = new teacherclass(); t853.name = "Marcus Emas"; t853.firstname = "Marcus"; t853.lastname = "Emas"; t853.teacherID = "mem"; t853.birthday = "750415"; teacherlist.Add(t853);
            teacherclass t854 = new teacherclass(); t854.name = "Margaretha Engwall"; t854.firstname = "Margaretha"; t854.lastname = "Engwall"; t854.teacherID = "men"; t854.birthday = "520101"; teacherlist.Add(t854);
            teacherclass t855 = new teacherclass(); t855.name = "Magnus Engström"; t855.firstname = "Magnus"; t855.lastname = "Engström"; t855.teacherID = "mes"; t855.birthday = "810106"; teacherlist.Add(t855);
            teacherclass t856 = new teacherclass(); t856.name = "Mattias Ellström"; t856.firstname = "Mattias"; t856.lastname = "Ellström"; t856.teacherID = "met"; t856.birthday = "960808"; teacherlist.Add(t856);
            teacherclass t857 = new teacherclass(); t857.name = "Mats Erixon"; t857.firstname = "Mats"; t857.lastname = "Erixon"; t857.teacherID = "mex"; t857.birthday = "530730"; teacherlist.Add(t857);
            teacherclass t858 = new teacherclass(); t858.name = "Mikael Fallqvist"; t858.firstname = "Mikael"; t858.lastname = "Fallqvist"; t858.teacherID = "mfa"; t858.birthday = "820519"; teacherlist.Add(t858);
            teacherclass t859 = new teacherclass(); t859.name = "Maria Fredriksson Sjöberg"; t859.firstname = "Maria"; t859.lastname = "Fredriksson Sjöberg"; t859.teacherID = "mfd"; t859.birthday = "790926"; teacherlist.Add(t859);
            teacherclass t860 = new teacherclass(); t860.name = "Maria Fernström"; t860.firstname = "Maria"; t860.lastname = "Fernström"; t860.teacherID = "mfe"; t860.birthday = "600826"; teacherlist.Add(t860);
            teacherclass t861 = new teacherclass(); t861.name = "Magnus Fahlström"; t861.firstname = "Magnus"; t861.lastname = "Fahlström"; t861.teacherID = "mfh"; t861.birthday = "710519"; teacherlist.Add(t861);
            teacherclass t862 = new teacherclass(); t862.name = "Majbritt Felleki"; t862.firstname = "Majbritt"; t862.lastname = "Felleki"; t862.teacherID = "mfl"; t862.birthday = "770531"; teacherlist.Add(t862);
            teacherclass t863 = new teacherclass(); t863.name = "Maria Forsner"; t863.firstname = "Maria"; t863.lastname = "Forsner"; t863.teacherID = "mfr"; t863.birthday = "540516"; teacherlist.Add(t863);
            teacherclass t864 = new teacherclass(); t864.name = "Mattias Gradén"; t864.firstname = "Mattias"; t864.lastname = "Gradén"; t864.teacherID = "mga"; t864.birthday = "740516"; teacherlist.Add(t864);
            teacherclass t865 = new teacherclass(); t865.name = "Mikael Grehk"; t865.firstname = "Mikael"; t865.lastname = "Grehk"; t865.teacherID = "mge"; t865.birthday = "621124"; teacherlist.Add(t865);
            teacherclass t866 = new teacherclass(); t866.name = "Maria Gräfnings"; t866.firstname = "Maria"; t866.lastname = "Gräfnings"; t866.teacherID = "mgn"; t866.birthday = "851029"; teacherlist.Add(t866);
            teacherclass t867 = new teacherclass(); t867.name = "Maria Görts"; t867.firstname = "Maria"; t867.lastname = "Görts"; t867.teacherID = "mgo"; t867.birthday = "560229"; teacherlist.Add(t867);
            teacherclass t868 = new teacherclass(); t868.name = "Maud Granberg"; t868.firstname = "Maud"; t868.lastname = "Granberg"; t868.teacherID = "mgr"; t868.birthday = "510330"; teacherlist.Add(t868);
            teacherclass t869 = new teacherclass(); t869.name = "Mikael Gustavsson"; t869.firstname = "Mikael"; t869.lastname = "Gustavsson"; t869.teacherID = "mgt"; t869.birthday = "650702"; teacherlist.Add(t869);
            teacherclass t870 = new teacherclass(); t870.name = "Marcus Gustafsson"; t870.firstname = "Marcus"; t870.lastname = "Gustafsson"; t870.teacherID = "mgu"; t870.birthday = "870209"; teacherlist.Add(t870);
            teacherclass t871 = new teacherclass(); t871.name = "Marika Hagelberg"; t871.firstname = "Marika"; t871.lastname = "Hagelberg"; t871.teacherID = "mhb"; t871.birthday = "800329"; teacherlist.Add(t871);
            teacherclass t872 = new teacherclass(); t872.name = "Maria Hedberg-Wänn"; t872.firstname = "Maria"; t872.lastname = "Hedberg-Wänn"; t872.teacherID = "mhd"; t872.birthday = "520806"; teacherlist.Add(t872);
            teacherclass t873 = new teacherclass(); t873.name = "Marit Halldén"; t873.firstname = "Marit"; t873.lastname = "Halldén"; t873.teacherID = "mhe"; t873.birthday = "600908"; teacherlist.Add(t873);
            teacherclass t874 = new teacherclass(); t874.name = "Marita Hilliges"; t874.firstname = "Marita"; t874.lastname = "Hilliges"; t874.teacherID = "mhi"; t874.birthday = "631127"; teacherlist.Add(t874);
            teacherclass t875 = new teacherclass(); t875.name = "Maria Westman"; t875.firstname = "Maria"; t875.lastname = "Westman"; t875.teacherID = "mhl"; t875.birthday = "630506"; teacherlist.Add(t875);
            teacherclass t876 = new teacherclass(); t876.name = "Marie Hagman"; t876.firstname = "Marie"; t876.lastname = "Hagman"; t876.teacherID = "mhm"; t876.birthday = "651031"; teacherlist.Add(t876);
            teacherclass t877 = new teacherclass(); t877.name = "Magnus Höglund"; t877.firstname = "Magnus"; t877.lastname = "Höglund"; t877.teacherID = "mho"; t877.birthday = "660831"; teacherlist.Add(t877);
            teacherclass t878 = new teacherclass(); t878.name = "Mathias Hatakka"; t878.firstname = "Mathias"; t878.lastname = "Hatakka"; t878.teacherID = "mht"; t878.birthday = "740627"; teacherlist.Add(t878);
            teacherclass t879 = new teacherclass(); t879.name = "Mårten Hugosson"; t879.firstname = "Mårten"; t879.lastname = "Hugosson"; t879.teacherID = "mhu"; t879.birthday = "580927"; teacherlist.Add(t879);
            teacherclass t880 = new teacherclass(); t880.name = "Marco Hernandez Velasco"; t880.firstname = "Marco"; t880.lastname = "Hernandez Velasco"; t880.teacherID = "mhv"; t880.birthday = "850206"; teacherlist.Add(t880);
            teacherclass t881 = new teacherclass(); t881.name = "Mikael Berglund"; t881.firstname = "Mikael"; t881.lastname = "Berglund"; t881.teacherID = "mib"; t881.birthday = "651209"; teacherlist.Add(t881);
            teacherclass t882 = new teacherclass(); t882.name = "Mikael Heed"; t882.firstname = "Mikael"; t882.lastname = "Heed"; t882.teacherID = "mih"; t882.birthday = "730930"; teacherlist.Add(t882);
            teacherclass t883 = new teacherclass(); t883.name = "Michael Nilsson"; t883.firstname = "Michael"; t883.lastname = "Nilsson"; t883.teacherID = "mii"; t883.birthday = "570525"; teacherlist.Add(t883);
            teacherclass t884 = new teacherclass(); t884.name = "Miyoko Inoue"; t884.firstname = "Miyoko"; t884.lastname = "Inoue"; t884.teacherID = "mio"; t884.birthday = "740814"; teacherlist.Add(t884);
            teacherclass t885 = new teacherclass(); t885.name = "Mikael Jansson Ahlnander"; t885.firstname = "Mikael"; t885.lastname = "Jansson Ahlnander"; t885.teacherID = "mja"; t885.birthday = "640731"; teacherlist.Add(t885);
            teacherclass t886 = new teacherclass(); t886.name = "Magnus Jobs"; t886.firstname = "Magnus"; t886.lastname = "Jobs"; t886.teacherID = "mjb"; t886.birthday = "690415"; teacherlist.Add(t886);
            teacherclass t887 = new teacherclass(); t887.name = "Madelene Johansen"; t887.firstname = "Madelene"; t887.lastname = "Johansen"; t887.teacherID = "mje"; t887.birthday = "620831"; teacherlist.Add(t887);
            teacherclass t888 = new teacherclass(); t888.name = "Magnus Jegermalm"; t888.firstname = "Magnus"; t888.lastname = "Jegermalm"; t888.teacherID = "mjg"; t888.birthday = "620417"; teacherlist.Add(t888);
            teacherclass t889 = new teacherclass(); t889.name = "Margareta Johansson"; t889.firstname = "Margareta"; t889.lastname = "Johansson"; t889.teacherID = "mjh"; t889.birthday = "640821"; teacherlist.Add(t889);
            teacherclass t890 = new teacherclass(); t890.name = "Markku Jääskeläinen"; t890.firstname = "Markku"; t890.lastname = "Jääskeläinen"; t890.teacherID = "mjk"; t890.birthday = "700212"; teacherlist.Add(t890);
            teacherclass t891 = new teacherclass(); t891.name = "Monika Jansson"; t891.firstname = "Monika"; t891.lastname = "Jansson"; t891.teacherID = "mjn"; t891.birthday = "620328"; teacherlist.Add(t891);
            teacherclass t892 = new teacherclass(); t892.name = "Martin Johanson"; t892.firstname = "Martin"; t892.lastname = "Johanson"; t892.teacherID = "mjoh"; t892.birthday = "600610"; teacherlist.Add(t892);
            teacherclass t893 = new teacherclass(); t893.name = "Mikael Jorhult"; t893.firstname = "Mikael"; t893.lastname = "Jorhult"; t893.teacherID = "mjr"; t893.birthday = "850713"; teacherlist.Add(t893);
            teacherclass t894 = new teacherclass(); t894.name = "Ängnas Maria Kvarnström"; t894.firstname = "Ängnas Maria"; t894.lastname = "Kvarnström"; t894.teacherID = "mka"; t894.birthday = "710912"; teacherlist.Add(t894);
            teacherclass t895 = new teacherclass(); t895.name = "Marilou Kooistra"; t895.firstname = "Marilou"; t895.lastname = "Kooistra"; t895.teacherID = "mkh"; t895.birthday = "521106"; teacherlist.Add(t895);
            teacherclass t896 = new teacherclass(); t896.name = "Marie Klingberg Allvin"; t896.firstname = "Marie"; t896.lastname = "Klingberg Allvin"; t896.teacherID = "mkl"; t896.birthday = "720726"; teacherlist.Add(t896);
            teacherclass t897 = new teacherclass(); t897.name = "Magnus Knutsson"; t897.firstname = "Magnus"; t897.lastname = "Knutsson"; t897.teacherID = "mkn"; t897.birthday = "630312"; teacherlist.Add(t897);
            teacherclass t898 = new teacherclass(); t898.name = "Maria Anna Melin (Kujawinska)"; t898.firstname = "Maria Anna"; t898.lastname = "Melin (Kujawinska)"; t898.teacherID = "mku"; t898.birthday = "840703"; teacherlist.Add(t898);
            teacherclass t899 = new teacherclass(); t899.name = "Mario Lopez-Cordero"; t899.firstname = "Mario"; t899.lastname = "Lopez-Cordero"; t899.teacherID = "mlc"; t899.birthday = "730804"; teacherlist.Add(t899);
            teacherclass t900 = new teacherclass(); t900.name = "Michael Lindgren"; t900.firstname = "Michael"; t900.lastname = "Lindgren"; t900.teacherID = "mld"; t900.birthday = "701205"; teacherlist.Add(t900);
            teacherclass t901 = new teacherclass(); t901.name = "Margareta Litsmark Forsgren"; t901.firstname = "Margareta"; t901.lastname = "Litsmark Forsgren"; t901.teacherID = "mlf"; t901.birthday = "540215"; teacherlist.Add(t901);
            teacherclass t902 = new teacherclass(); t902.name = "Michael Lindgren"; t902.firstname = "Michael"; t902.lastname = "Lindgren"; t902.teacherID = "mlg"; t902.birthday = "690111"; teacherlist.Add(t902);
            teacherclass t903 = new teacherclass(); t903.name = "Madelen Lagin"; t903.firstname = "Madelen"; t903.lastname = "Lagin"; t903.teacherID = "mli"; t903.birthday = "820130"; teacherlist.Add(t903);
            teacherclass t904 = new teacherclass(); t904.name = "Maria Lejskog"; t904.firstname = "Maria"; t904.lastname = "Lejskog"; t904.teacherID = "mlj"; t904.birthday = "770429"; teacherlist.Add(t904);
            teacherclass t905 = new teacherclass(); t905.name = "Mats Lundmark"; t905.firstname = "Mats"; t905.lastname = "Lundmark"; t905.teacherID = "mlk"; t905.birthday = "570411"; teacherlist.Add(t905);
            teacherclass t906 = new teacherclass(); t906.name = "Martina Ladendorf"; t906.firstname = "Martina"; t906.lastname = "Ladendorf"; t906.teacherID = "mln"; t906.birthday = "700326"; teacherlist.Add(t906);
            teacherclass t907 = new teacherclass(); t907.name = "Mikael Lotsengård"; t907.firstname = "Mikael"; t907.lastname = "Lotsengård"; t907.teacherID = "mlo"; t907.birthday = "751027"; teacherlist.Add(t907);
            teacherclass t908 = new teacherclass(); t908.name = "Martin Litens"; t908.firstname = "Martin"; t908.lastname = "Litens"; t908.teacherID = "mlt"; t908.birthday = "670418"; teacherlist.Add(t908);
            teacherclass t909 = new teacherclass(); t909.name = "Mats Lundgren"; t909.firstname = "Mats"; t909.lastname = "Lundgren"; t909.teacherID = "mlu"; t909.birthday = "490207"; teacherlist.Add(t909);
            teacherclass t910 = new teacherclass(); t910.name = "Malin Lövgren"; t910.firstname = "Malin"; t910.lastname = "Lövgren"; t910.teacherID = "mlv"; t910.birthday = "800707"; teacherlist.Add(t910);
            teacherclass t911 = new teacherclass(); t911.name = "Margareta Mcleod"; t911.firstname = "Margareta"; t911.lastname = "Mcleod"; t911.teacherID = "mmc"; t911.birthday = "470802"; teacherlist.Add(t911);
            teacherclass t912 = new teacherclass(); t912.name = "Michael Malmhed"; t912.firstname = "Michael"; t912.lastname = "Malmhed"; t912.teacherID = "mmd"; t912.birthday = "571002"; teacherlist.Add(t912);
            teacherclass t913 = new teacherclass(); t913.name = "Magdalena Mattebo"; t913.firstname = "Magdalena"; t913.lastname = "Mattebo"; t913.teacherID = "mme"; t913.birthday = "760901"; teacherlist.Add(t913);
            teacherclass t914 = new teacherclass(); t914.name = "Mikael Magnusson"; t914.firstname = "Mikael"; t914.lastname = "Magnusson"; t914.teacherID = "mmg"; t914.birthday = "671211"; teacherlist.Add(t914);
            teacherclass t915 = new teacherclass(); t915.name = "Memedi Mevludin"; t915.firstname = "Memedi"; t915.lastname = "Mevludin"; t915.teacherID = "mmi"; t915.birthday = "831019"; teacherlist.Add(t915);
            teacherclass t916 = new teacherclass(); t916.name = "Marcia Markus"; t916.firstname = "Marcia"; t916.lastname = "Markus"; t916.teacherID = "mmk"; t916.birthday = "670515"; teacherlist.Add(t916);
            teacherclass t917 = new teacherclass(); t917.name = "Margareta Malmgren-Scholz"; t917.firstname = "Margareta"; t917.lastname = "Malmgren-Scholz"; t917.teacherID = "mmm"; t917.birthday = "480317"; teacherlist.Add(t917);
            teacherclass t918 = new teacherclass(); t918.name = "Maria Moberg"; t918.firstname = "Maria"; t918.lastname = "Moberg"; t918.teacherID = "mmo"; t918.birthday = "570518"; teacherlist.Add(t918);
            teacherclass t919 = new teacherclass(); t919.name = "Marika Marusarz"; t919.firstname = "Marika"; t919.lastname = "Marusarz"; t919.teacherID = "mmr"; t919.birthday = "530811"; teacherlist.Add(t919);
            teacherclass t920 = new teacherclass(); t920.name = "Maria Masgård"; t920.firstname = "Maria"; t920.lastname = "Masgård"; t920.teacherID = "mms"; t920.birthday = "661230"; teacherlist.Add(t920);
            teacherclass t921 = new teacherclass(); t921.name = "Marie Moström Åberg"; t921.firstname = "Marie"; t921.lastname = "Moström Åberg"; t921.teacherID = "mmt"; t921.birthday = "690517"; teacherlist.Add(t921);
            teacherclass t922 = new teacherclass(); t922.name = "Monika Matevska Stier"; t922.firstname = "Monika"; t922.lastname = "Matevska Stier"; t922.teacherID = "mmv"; t922.birthday = "710206"; teacherlist.Add(t922);
            teacherclass t923 = new teacherclass(); t923.name = "Mahwish Naseer"; t923.firstname = "Mahwish"; t923.lastname = "Naseer"; t923.teacherID = "mna"; t923.birthday = "850116"; teacherlist.Add(t923);
            teacherclass t924 = new teacherclass(); t924.name = "Mariya Aida Niendorf"; t924.firstname = "Mariya Aida"; t924.lastname = "Niendorf"; t924.teacherID = "mni"; t924.birthday = "680629"; teacherlist.Add(t924);
            teacherclass t925 = new teacherclass(); t925.name = "Martin Nykvist Drotz"; t925.firstname = "Martin"; t925.lastname = "Nykvist Drotz"; t925.teacherID = "mnk"; t925.birthday = "851014"; teacherlist.Add(t925);
            teacherclass t926 = new teacherclass(); t926.name = "Maria Neljesjö"; t926.firstname = "Maria"; t926.lastname = "Neljesjö"; t926.teacherID = "mnl"; t926.birthday = "640917"; teacherlist.Add(t926);
            teacherclass t927 = new teacherclass(); t927.name = "Maria Nilsson"; t927.firstname = "Maria"; t927.lastname = "Nilsson"; t927.teacherID = "mnn"; t927.birthday = "840517"; teacherlist.Add(t927);
            teacherclass t928 = new teacherclass(); t928.name = "Marit Stub Nybelius"; t928.firstname = "Marit"; t928.lastname = "Stub Nybelius"; t928.teacherID = "mny"; t928.birthday = "721218"; teacherlist.Add(t928);
            teacherclass t929 = new teacherclass(); t929.name = "Marie Olsen"; t929.firstname = "Marie"; t929.lastname = "Olsen"; t929.teacherID = "moe"; t929.birthday = "670128"; teacherlist.Add(t929);
            teacherclass t930 = new teacherclass(); t930.name = "Monica Eriksson"; t930.firstname = "Monica"; t930.lastname = "Eriksson"; t930.teacherID = "moer"; t930.birthday = "650611"; teacherlist.Add(t930);
            teacherclass t931 = new teacherclass(); t931.name = "Monica Grönvall"; t931.firstname = "Monica"; t931.lastname = "Grönvall"; t931.teacherID = "mogr"; t931.birthday = "641107"; teacherlist.Add(t931);
            teacherclass t932 = new teacherclass(); t932.name = "Matts Öhrn"; t932.firstname = "Matts"; t932.lastname = "Öhrn"; t932.teacherID = "moh"; t932.birthday = "520303"; teacherlist.Add(t932);
            teacherclass t933 = new teacherclass(); t933.name = "Monique Toratti Lindgren"; t933.firstname = "Monique"; t933.lastname = "Toratti Lindgren"; t933.teacherID = "moi"; t933.birthday = "610902"; teacherlist.Add(t933);
            teacherclass t934 = new teacherclass(); t934.name = "Mikael Olsson"; t934.firstname = "Mikael"; t934.lastname = "Olsson"; t934.teacherID = "mol"; t934.birthday = "600928"; teacherlist.Add(t934);
            teacherclass t935 = new teacherclass(); t935.name = "Michael Oppenheimer"; t935.firstname = "Michael"; t935.lastname = "Oppenheimer"; t935.teacherID = "mom"; t935.birthday = "760827"; teacherlist.Add(t935);
            teacherclass t936 = new teacherclass(); t936.name = "Mats Olsson"; t936.firstname = "Mats"; t936.lastname = "Olsson"; t936.teacherID = "mon"; t936.birthday = "860412"; teacherlist.Add(t936);
            teacherclass t937 = new teacherclass(); t937.name = "Maria Olsson"; t937.firstname = "Maria"; t937.lastname = "Olsson"; t937.teacherID = "moo"; t937.birthday = "611226"; teacherlist.Add(t937);
            teacherclass t938 = new teacherclass(); t938.name = "Mia Österberg"; t938.firstname = "Mia"; t938.lastname = "Österberg"; t938.teacherID = "mot"; t938.birthday = "781229"; teacherlist.Add(t938);
            teacherclass t939 = new teacherclass(); t939.name = "Mohammad Parhizgar"; t939.firstname = "Mohammad"; t939.lastname = "Parhizgar"; t939.teacherID = "mpa"; t939.birthday = "540528"; teacherlist.Add(t939);
            teacherclass t940 = new teacherclass(); t940.name = "Marianne Pettersson"; t940.firstname = "Marianne"; t940.lastname = "Pettersson"; t940.teacherID = "mpe"; t940.birthday = "501231"; teacherlist.Add(t940);
            teacherclass t941 = new teacherclass(); t941.name = "Margareta Persson"; t941.firstname = "Margareta"; t941.lastname = "Persson"; t941.teacherID = "mpo"; t941.birthday = "580820"; teacherlist.Add(t941);
            teacherclass t942 = new teacherclass(); t942.name = "Maria Lindstens"; t942.firstname = "Maria"; t942.lastname = "Lindstens"; t942.teacherID = "mpr"; t942.birthday = "791205"; teacherlist.Add(t942);
            teacherclass t943 = new teacherclass(); t943.name = "Madeleine Perrault"; t943.firstname = "Madeleine"; t943.lastname = "Perrault"; t943.teacherID = "mpu"; t943.birthday = "841228"; teacherlist.Add(t943);
            teacherclass t944 = new teacherclass(); t944.name = "Mats Rönnelid"; t944.firstname = "Mats"; t944.lastname = "Rönnelid"; t944.teacherID = "mrd"; t944.birthday = "610303"; teacherlist.Add(t944);
            teacherclass t945 = new teacherclass(); t945.name = "Marit Ragnarsson"; t945.firstname = "Marit"; t945.lastname = "Ragnarsson"; t945.teacherID = "mrg"; t945.birthday = "630125"; teacherlist.Add(t945);
            teacherclass t946 = new teacherclass(); t946.name = "Margareta Ribjer"; t946.firstname = "Margareta"; t946.lastname = "Ribjer"; t946.teacherID = "mri"; t946.birthday = "530415"; teacherlist.Add(t946);
            teacherclass t947 = new teacherclass(); t947.name = "Maria Rantanen"; t947.firstname = "Maria"; t947.lastname = "Rantanen"; t947.teacherID = "mrn"; t947.birthday = "670830"; teacherlist.Add(t947);
            teacherclass t948 = new teacherclass(); t948.name = "Mats Roslund"; t948.firstname = "Mats"; t948.lastname = "Roslund"; t948.teacherID = "mro"; t948.birthday = "580127"; teacherlist.Add(t948);
            teacherclass t949 = new teacherclass(); t949.name = "Sori Rasti"; t949.firstname = "Sori"; t949.lastname = "Rasti"; t949.teacherID = "mrs"; t949.birthday = "600823"; teacherlist.Add(t949);
            teacherclass t950 = new teacherclass(); t950.name = "Malin Roitman"; t950.firstname = "Malin"; t950.lastname = "Roitman"; t950.teacherID = "mrt"; t950.birthday = "680930"; teacherlist.Add(t950);
            teacherclass t951 = new teacherclass(); t951.name = "Mikael Strömberg"; t951.firstname = "Mikael"; t951.lastname = "Strömberg"; t951.teacherID = "msb"; t951.birthday = "571208"; teacherlist.Add(t951);
            teacherclass t952 = new teacherclass(); t952.name = "Monica Sandbacka"; t952.firstname = "Monica"; t952.lastname = "Sandbacka"; t952.teacherID = "msc"; t952.birthday = "610923"; teacherlist.Add(t952);
            teacherclass t953 = new teacherclass(); t953.name = "Maria Svedbo Engström"; t953.firstname = "Maria"; t953.lastname = "Svedbo Engström"; t953.teacherID = "msd"; t953.birthday = "801106"; teacherlist.Add(t953);
            teacherclass t954 = new teacherclass(); t954.name = "Martin Salzmann-Erikson"; t954.firstname = "Martin"; t954.lastname = "Salzmann-Erikson"; t954.teacherID = "mse"; t954.birthday = "771107"; teacherlist.Add(t954);
            teacherclass t955 = new teacherclass(); t955.name = "Mario Semiao"; t955.firstname = "Mario"; t955.lastname = "Semiao"; t955.teacherID = "msi"; t955.birthday = "801119"; teacherlist.Add(t955);
            teacherclass t956 = new teacherclass(); t956.name = "Monireh Sajadpour"; t956.firstname = "Monireh"; t956.lastname = "Sajadpour"; t956.teacherID = "msj"; t956.birthday = "860916"; teacherlist.Add(t956);
            teacherclass t957 = new teacherclass(); t957.name = "Mikael Stille"; t957.firstname = "Mikael"; t957.lastname = "Stille"; t957.teacherID = "msl"; t957.birthday = "730312"; teacherlist.Add(t957);
            teacherclass t958 = new teacherclass(); t958.name = "Marie Ericson"; t958.firstname = "Marie"; t958.lastname = "Ericson"; t958.teacherID = "msm"; t958.birthday = "751229"; teacherlist.Add(t958);
            teacherclass t959 = new teacherclass(); t959.name = "Maria Svensson"; t959.firstname = "Maria"; t959.lastname = "Svensson"; t959.teacherID = "msn"; t959.birthday = "670421"; teacherlist.Add(t959);
            teacherclass t960 = new teacherclass(); t960.name = "Mats Sjögren"; t960.firstname = "Mats"; t960.lastname = "Sjögren"; t960.teacherID = "mso"; t960.birthday = "770505"; teacherlist.Add(t960);
            teacherclass t961 = new teacherclass(); t961.name = "Marianne Spante"; t961.firstname = "Marianne"; t961.lastname = "Spante"; t961.teacherID = "msp"; t961.birthday = "580727"; teacherlist.Add(t961);
            teacherclass t962 = new teacherclass(); t962.name = "Martin Sandström Rimsbo"; t962.firstname = "Martin"; t962.lastname = "Sandström Rimsbo"; t962.teacherID = "mss"; t962.birthday = "770517"; teacherlist.Add(t962);
            teacherclass t963 = new teacherclass(); t963.name = "Monika Stridfeldt"; t963.firstname = "Monika"; t963.lastname = "Stridfeldt"; t963.teacherID = "mst"; t963.birthday = "720417"; teacherlist.Add(t963);
            teacherclass t964 = new teacherclass(); t964.name = "Maria Sundberg"; t964.firstname = "Maria"; t964.lastname = "Sundberg"; t964.teacherID = "msu"; t964.birthday = "710323"; teacherlist.Add(t964);
            teacherclass t965 = new teacherclass(); t965.name = "Mohammed Tahir"; t965.firstname = "Mohammed"; t965.lastname = "Tahir"; t965.teacherID = "mta"; t965.birthday = "610603"; teacherlist.Add(t965);
            teacherclass t966 = new teacherclass(); t966.name = "Mats Tegmark"; t966.firstname = "Mats"; t966.lastname = "Tegmark"; t966.teacherID = "mte"; t966.birthday = "660513"; teacherlist.Add(t966);
            teacherclass t967 = new teacherclass(); t967.name = "Maria Thulemark"; t967.firstname = "Maria"; t967.lastname = "Thulemark"; t967.teacherID = "mth"; t967.birthday = "820803"; teacherlist.Add(t967);
            teacherclass t968 = new teacherclass(); t968.name = "Malin Tistad"; t968.firstname = "Malin"; t968.lastname = "Tistad"; t968.teacherID = "mti"; t968.birthday = "700810"; teacherlist.Add(t968);
            teacherclass t969 = new teacherclass(); t969.name = "Michail Tonkonogi"; t969.firstname = "Michail"; t969.lastname = "Tonkonogi"; t969.teacherID = "mtn"; t969.birthday = "670429"; teacherlist.Add(t969);
            teacherclass t970 = new teacherclass(); t970.name = "Michael Toivio"; t970.firstname = "Michael"; t970.lastname = "Toivio"; t970.teacherID = "mto"; t970.birthday = "591028"; teacherlist.Add(t970);
            teacherclass t971 = new teacherclass(); t971.name = "Masako Hayakawa Thor"; t971.firstname = "Masako"; t971.lastname = "Hayakawa Thor"; t971.teacherID = "mtr"; t971.birthday = "671113"; teacherlist.Add(t971);
            teacherclass t972 = new teacherclass(); t972.name = "Maria Taxell-Stoltz"; t972.firstname = "Maria"; t972.lastname = "Taxell-Stoltz"; t972.teacherID = "mtx"; t972.birthday = "770824"; teacherlist.Add(t972);
            teacherclass t973 = new teacherclass(); t973.name = "Mikael Wiberg"; t973.firstname = "Mikael"; t973.lastname = "Wiberg"; t973.teacherID = "mvb"; t973.birthday = "830507"; teacherlist.Add(t973);
            teacherclass t974 = new teacherclass(); t974.name = "Mikael Wiberg"; t974.firstname = "Mikael"; t974.lastname = "Wiberg"; t974.teacherID = "mwb"; t974.birthday = "820608"; teacherlist.Add(t974);
            teacherclass t975 = new teacherclass(); t975.name = "Marianne Vemhäll"; t975.firstname = "Marianne"; t975.lastname = "Vemhäll"; t975.teacherID = "mve"; t975.birthday = "501128"; teacherlist.Add(t975);
            teacherclass t976 = new teacherclass(); t976.name = "Mattias Vikman"; t976.firstname = "Mattias"; t976.lastname = "Vikman"; t976.teacherID = "mvi"; t976.birthday = "730310"; teacherlist.Add(t976);
            teacherclass t977 = new teacherclass(); t977.name = "Mia Wickander"; t977.firstname = "Mia"; t977.lastname = "Wickander"; t977.teacherID = "mwi"; t977.birthday = "731026"; teacherlist.Add(t977);
            teacherclass t978 = new teacherclass(); t978.name = "Monika Vinterek"; t978.firstname = "Monika"; t978.lastname = "Vinterek"; t978.teacherID = "mvn"; t978.birthday = "560303"; teacherlist.Add(t978);
            teacherclass t979 = new teacherclass(); t979.name = "Maria Wallinder"; t979.firstname = "Maria"; t979.lastname = "Wallinder"; t979.teacherID = "mwn"; t979.birthday = "740711"; teacherlist.Add(t979);
            teacherclass t980 = new teacherclass(); t980.name = "Mark Warner"; t980.firstname = "Mark"; t980.lastname = "Warner"; t980.teacherID = "mwr"; t980.birthday = "740327"; teacherlist.Add(t980);
            teacherclass t981 = new teacherclass(); t981.name = "Nathalie Andersson"; t981.firstname = "Nathalie"; t981.lastname = "Andersson"; t981.teacherID = "nan"; t981.birthday = "900430"; teacherlist.Add(t981);
            teacherclass t982 = new teacherclass(); t982.name = "Nina Bengtsson"; t982.firstname = "Nina"; t982.lastname = "Bengtsson"; t982.teacherID = "nbg"; t982.birthday = "721225"; teacherlist.Add(t982);
            teacherclass t983 = new teacherclass(); t983.name = "Nora Bencivenni"; t983.firstname = "Nora"; t983.lastname = "Bencivenni"; t983.teacherID = "nbn"; t983.birthday = "800915"; teacherlist.Add(t983);
            teacherclass t984 = new teacherclass(); t984.name = "Niclas Arkåsen"; t984.firstname = "Niclas"; t984.lastname = "Arkåsen"; t984.teacherID = "ner"; t984.birthday = "780628"; teacherlist.Add(t984);
            teacherclass t985 = new teacherclass(); t985.name = "Nina Fällstrand Larsson"; t985.firstname = "Nina"; t985.lastname = "Fällstrand Larsson"; t985.teacherID = "nfl"; t985.birthday = "681123"; teacherlist.Add(t985);
            teacherclass t986 = new teacherclass(); t986.name = "Niklas Hermansson"; t986.firstname = "Niklas"; t986.lastname = "Hermansson"; t986.teacherID = "nhe"; t986.birthday = "640908"; teacherlist.Add(t986);
            teacherclass t987 = new teacherclass(); t987.name = "Nils Johansson"; t987.firstname = "Nils"; t987.lastname = "Johansson"; t987.teacherID = "njh"; t987.birthday = "870116"; teacherlist.Add(t987);
            teacherclass t988 = new teacherclass(); t988.name = "Ingrid Jonsson Wallin"; t988.firstname = "Ingrid"; t988.lastname = "Jonsson Wallin"; t988.teacherID = "njw"; t988.birthday = "520416"; teacherlist.Add(t988);
            teacherclass t989 = new teacherclass(); t989.name = "Li Na"; t989.firstname = "Li"; t989.lastname = "Na"; t989.teacherID = "nla"; t989.birthday = "560308"; teacherlist.Add(t989);
            teacherclass t990 = new teacherclass(); t990.name = "Nadezda Lebedeva"; t990.firstname = "Nadezda"; t990.lastname = "Lebedeva"; t990.teacherID = "nle"; t990.birthday = "781221"; teacherlist.Add(t990);
            teacherclass t991 = new teacherclass(); t991.name = "Niklas Lindegård"; t991.firstname = "Niklas"; t991.lastname = "Lindegård"; t991.teacherID = "nli"; t991.birthday = "760206"; teacherlist.Add(t991);
            teacherclass t992 = new teacherclass(); t992.name = "Nike Leima"; t992.firstname = "Nike"; t992.lastname = "Leima"; t992.teacherID = "nlm"; t992.birthday = "760722"; teacherlist.Add(t992);
            teacherclass t993 = new teacherclass(); t993.name = "Lars-Olof Nordqvist"; t993.firstname = "Lars-Olof"; t993.lastname = "Nordqvist"; t993.teacherID = "nlo"; t993.birthday = "520415"; teacherlist.Add(t993);
            teacherclass t994 = new teacherclass(); t994.name = "Nikita Mikhaylov"; t994.firstname = "Nikita"; t994.lastname = "Mikhaylov"; t994.teacherID = "nmi"; t994.birthday = "750405"; teacherlist.Add(t994);
            teacherclass t995 = new teacherclass(); t995.name = "Nicola Nerström"; t995.firstname = "Nicola"; t995.lastname = "Nerström"; t995.teacherID = "nne"; t995.birthday = "590216"; teacherlist.Add(t995);
            teacherclass t996 = new teacherclass(); t996.name = "Niklas Rudholm"; t996.firstname = "Niklas"; t996.lastname = "Rudholm"; t996.teacherID = "nru"; t996.birthday = "710312"; teacherlist.Add(t996);
            teacherclass t997 = new teacherclass(); t997.name = "Neta Rydén"; t997.firstname = "Neta"; t997.lastname = "Rydén"; t997.teacherID = "nry"; t997.birthday = "651127"; teacherlist.Add(t997);
            teacherclass t998 = new teacherclass(); t998.name = "Nigar Sadig"; t998.firstname = "Nigar"; t998.lastname = "Sadig"; t998.teacherID = "nsa"; t998.birthday = "760111"; teacherlist.Add(t998);
            teacherclass t999 = new teacherclass(); t999.name = "Nima Nosar Safara"; t999.firstname = "Nima"; t999.lastname = "Nosar Safara"; t999.teacherID = "nsn"; t999.birthday = "820922"; teacherlist.Add(t999);
            teacherclass t1000 = new teacherclass(); t1000.name = "Ninni Wallfelt"; t1000.firstname = "Ninni"; t1000.lastname = "Wallfelt"; t1000.teacherID = "nwa"; t1000.birthday = "650206"; teacherlist.Add(t1000);
            teacherclass t1001 = new teacherclass(); t1001.name = "Nian Zhou"; t1001.firstname = "Nian"; t1001.lastname = "Zhou"; t1001.teacherID = "nzh"; t1001.birthday = "841021"; teacherlist.Add(t1001);
            teacherclass t1002 = new teacherclass(); t1002.name = "Olof Hansson"; t1002.firstname = "Olof"; t1002.lastname = "Hansson"; t1002.teacherID = "oha"; t1002.birthday = "860830"; teacherlist.Add(t1002);
            teacherclass t1003 = new teacherclass(); t1003.name = "Olavi Hemmilä"; t1003.firstname = "Olavi"; t1003.lastname = "Hemmilä"; t1003.teacherID = "ohe"; t1003.birthday = "570419"; teacherlist.Add(t1003);
            teacherclass t1004 = new teacherclass(); t1004.name = "Olga Mattsson"; t1004.firstname = "Olga"; t1004.lastname = "Mattsson"; t1004.teacherID = "oma"; t1004.birthday = "811029"; teacherlist.Add(t1004);
            teacherclass t1005 = new teacherclass(); t1005.name = "Oana Mihaescu"; t1005.firstname = "Oana"; t1005.lastname = "Mihaescu"; t1005.teacherID = "omi"; t1005.birthday = "771215"; teacherlist.Add(t1005);
            teacherclass t1006 = new teacherclass(); t1006.name = "Ola Nääs"; t1006.firstname = "Ola"; t1006.lastname = "Nääs"; t1006.teacherID = "ona"; t1006.birthday = "531005"; teacherlist.Add(t1006);
            teacherclass t1007 = new teacherclass(); t1007.name = "Ola Norrbelius"; t1007.firstname = "Ola"; t1007.lastname = "Norrbelius"; t1007.teacherID = "ono"; t1007.birthday = "510809"; teacherlist.Add(t1007);
            teacherclass t1008 = new teacherclass(); t1008.name = "Olivia Örtlund"; t1008.firstname = "Olivia"; t1008.lastname = "Örtlund"; t1008.teacherID = "oor"; t1008.birthday = "701004"; teacherlist.Add(t1008);
            teacherclass t1009 = new teacherclass(); t1009.name = "Sven-Åke Jåfs"; t1009.firstname = "Sven-Åke"; t1009.lastname = "Jåfs"; t1009.teacherID = "ork"; t1009.birthday = "460609"; teacherlist.Add(t1009);
            teacherclass t1010 = new teacherclass(); t1010.name = "Ola Rutz"; t1010.firstname = "Ola"; t1010.lastname = "Rutz"; t1010.teacherID = "oru"; t1010.birthday = "620410"; teacherlist.Add(t1010);
            teacherclass t1011 = new teacherclass(); t1011.name = "Ove Sundmark"; t1011.firstname = "Ove"; t1011.lastname = "Sundmark"; t1011.teacherID = "osu"; t1011.birthday = "610406"; teacherlist.Add(t1011);
            teacherclass t1012 = new teacherclass(); t1012.name = "Olga Viberg"; t1012.firstname = "Olga"; t1012.lastname = "Viberg"; t1012.teacherID = "ovi"; t1012.birthday = "820830"; teacherlist.Add(t1012);
            teacherclass t1013 = new teacherclass(); t1013.name = "Pierre Andersson"; t1013.firstname = "Pierre"; t1013.lastname = "Andersson"; t1013.teacherID = "pad"; t1013.birthday = "640727"; teacherlist.Add(t1013);
            teacherclass t1014 = new teacherclass(); t1014.name = "Anna Gudmundsson Hillman"; t1014.firstname = "Anna"; t1014.lastname = "Gudmundsson Hillman"; t1014.teacherID = "pag"; t1014.birthday = "740215"; teacherlist.Add(t1014);
            teacherclass t1015 = new teacherclass(); t1015.name = "Peter Åkerbäck"; t1015.firstname = "Peter"; t1015.lastname = "Åkerbäck"; t1015.teacherID = "pak"; t1015.birthday = "670125"; teacherlist.Add(t1015);
            teacherclass t1016 = new teacherclass(); t1016.name = "Patrik Arousell"; t1016.firstname = "Patrik"; t1016.lastname = "Arousell"; t1016.teacherID = "pao"; t1016.birthday = "700523"; teacherlist.Add(t1016);
            teacherclass t1017 = new teacherclass(); t1017.name = "Peter Berggren"; t1017.firstname = "Peter"; t1017.lastname = "Berggren"; t1017.teacherID = "pbe"; t1017.birthday = "670228"; teacherlist.Add(t1017);
            teacherclass t1018 = new teacherclass(); t1018.name = "Petter Börjesson"; t1018.firstname = "Petter"; t1018.lastname = "Börjesson"; t1018.teacherID = "pbo"; t1018.birthday = "680521"; teacherlist.Add(t1018);
            teacherclass t1019 = new teacherclass(); t1019.name = "Pia Berg"; t1019.firstname = "Pia"; t1019.lastname = "Berg"; t1019.teacherID = "pbr"; t1019.birthday = "550114"; teacherlist.Add(t1019);
            teacherclass t1020 = new teacherclass(); t1020.name = "Per Carlsson"; t1020.firstname = "Per"; t1020.lastname = "Carlsson"; t1020.teacherID = "pca"; t1020.birthday = "691001"; teacherlist.Add(t1020);
            teacherclass t1021 = new teacherclass(); t1021.name = "Peter Dobers"; t1021.firstname = "Peter"; t1021.lastname = "Dobers"; t1021.teacherID = "pdb"; t1021.birthday = "660912"; teacherlist.Add(t1021);
            teacherclass t1022 = new teacherclass(); t1022.name = "Per Dahl"; t1022.firstname = "Per"; t1022.lastname = "Dahl"; t1022.teacherID = "pdh"; t1022.birthday = "580718"; teacherlist.Add(t1022);
            teacherclass t1023 = new teacherclass(); t1023.name = "Pär Douhan"; t1023.firstname = "Pär"; t1023.lastname = "Douhan"; t1023.teacherID = "pdo"; t1023.birthday = "620217"; teacherlist.Add(t1023);
            teacherclass t1024 = new teacherclass(); t1024.name = "Per Edén"; t1024.firstname = "Per"; t1024.lastname = "Edén"; t1024.teacherID = "ped"; t1024.birthday = "530407"; teacherlist.Add(t1024);
            teacherclass t1025 = new teacherclass(); t1025.name = "Pär Eriksson"; t1025.firstname = "Pär"; t1025.lastname = "Eriksson"; t1025.teacherID = "pei"; t1025.birthday = "660611"; teacherlist.Add(t1025);
            teacherclass t1026 = new teacherclass(); t1026.name = "Per-Erik Eriksson"; t1026.firstname = "Per-Erik"; t1026.lastname = "Eriksson"; t1026.teacherID = "pek"; t1026.birthday = "710516"; teacherlist.Add(t1026);
            teacherclass t1027 = new teacherclass(); t1027.name = "Peter Enström"; t1027.firstname = "Peter"; t1027.lastname = "Enström"; t1027.teacherID = "pem"; t1027.birthday = "680512"; teacherlist.Add(t1027);
            teacherclass t1028 = new teacherclass(); t1028.name = "Paula Englund"; t1028.firstname = "Paula"; t1028.lastname = "Englund"; t1028.teacherID = "pen"; t1028.birthday = "760323"; teacherlist.Add(t1028);
            teacherclass t1029 = new teacherclass(); t1029.name = "Pia Eriksson"; t1029.firstname = "Pia"; t1029.lastname = "Eriksson"; t1029.teacherID = "per"; t1029.birthday = "600111"; teacherlist.Add(t1029);
            teacherclass t1030 = new teacherclass(); t1030.name = "Patrik Fackt"; t1030.firstname = "Patrik"; t1030.lastname = "Fackt"; t1030.teacherID = "pfa"; t1030.birthday = "781002"; teacherlist.Add(t1030);
            teacherclass t1031 = new teacherclass(); t1031.name = "Paul Flack"; t1031.firstname = "Paul"; t1031.lastname = "Flack"; t1031.teacherID = "pfl"; t1031.birthday = "740405"; teacherlist.Add(t1031);
            teacherclass t1032 = new teacherclass(); t1032.name = "Peter Gabrielsson"; t1032.firstname = "Peter"; t1032.lastname = "Gabrielsson"; t1032.teacherID = "pga"; t1032.birthday = "730301"; teacherlist.Add(t1032);
            teacherclass t1033 = new teacherclass(); t1033.name = "Per Granit"; t1033.firstname = "Per"; t1033.lastname = "Granit"; t1033.teacherID = "pgr"; t1033.birthday = "730505"; teacherlist.Add(t1033);
            teacherclass t1034 = new teacherclass(); t1034.name = "Pia Gustafsson"; t1034.firstname = "Pia"; t1034.lastname = "Gustafsson"; t1034.teacherID = "pgs"; t1034.birthday = "771121"; teacherlist.Add(t1034);
            teacherclass t1035 = new teacherclass(); t1035.name = "Peter Gustavsson Lidman"; t1035.firstname = "Peter"; t1035.lastname = "Gustavsson Lidman"; t1035.teacherID = "pgt"; t1035.birthday = "651112"; teacherlist.Add(t1035);
            teacherclass t1036 = new teacherclass(); t1036.name = "Petra Gustafsson"; t1036.firstname = "Petra"; t1036.lastname = "Gustafsson"; t1036.teacherID = "pgu"; t1036.birthday = "800717"; teacherlist.Add(t1036);
            teacherclass t1037 = new teacherclass(); t1037.name = "Per Håkansson"; t1037.firstname = "Per"; t1037.lastname = "Håkansson"; t1037.teacherID = "pha"; t1037.birthday = "760326"; teacherlist.Add(t1037);
            teacherclass t1038 = new teacherclass(); t1038.name = "Petra Hedgren"; t1038.firstname = "Petra"; t1038.lastname = "Hedgren"; t1038.teacherID = "phg"; t1038.birthday = "700214"; teacherlist.Add(t1038);
            teacherclass t1039 = new teacherclass(); t1039.name = "Pia Wallén"; t1039.firstname = "Pia"; t1039.lastname = "Wallén"; t1039.teacherID = "pia"; t1039.birthday = "610702"; teacherlist.Add(t1039);
            teacherclass t1040 = new teacherclass(); t1040.name = "Peter Jansson"; t1040.firstname = "Peter"; t1040.lastname = "Jansson"; t1040.teacherID = "pja"; t1040.birthday = "650726"; teacherlist.Add(t1040);
            teacherclass t1041 = new teacherclass(); t1041.name = "Pasi Juujärvi"; t1041.firstname = "Pasi"; t1041.lastname = "Juujärvi"; t1041.teacherID = "pju"; t1041.birthday = "630325"; teacherlist.Add(t1041);
            teacherclass t1042 = new teacherclass(); t1042.name = "Patrick Kenger"; t1042.firstname = "Patrick"; t1042.lastname = "Kenger"; t1042.teacherID = "pke"; t1042.birthday = "691103"; teacherlist.Add(t1042);
            teacherclass t1043 = new teacherclass(); t1043.name = "Petter Kolseth"; t1043.firstname = "Petter"; t1043.lastname = "Kolseth"; t1043.teacherID = "pkl"; t1043.birthday = "491119"; teacherlist.Add(t1043);
            teacherclass t1044 = new teacherclass(); t1044.name = "Patrik Karlqvist"; t1044.firstname = "Patrik"; t1044.lastname = "Karlqvist"; t1044.teacherID = "pkq"; t1044.birthday = "740821"; teacherlist.Add(t1044);
            teacherclass t1045 = new teacherclass(); t1045.name = "Patrik Karlsson"; t1045.firstname = "Patrik"; t1045.lastname = "Karlsson"; t1045.teacherID = "pkr"; t1045.birthday = "870318"; teacherlist.Add(t1045);
            teacherclass t1046 = new teacherclass(); t1046.name = "Paul Katsivelis"; t1046.firstname = "Paul"; t1046.lastname = "Katsivelis"; t1046.teacherID = "pkt"; t1046.birthday = "640815"; teacherlist.Add(t1046);
            teacherclass t1047 = new teacherclass(); t1047.name = "Pernilla Liedgren"; t1047.firstname = "Pernilla"; t1047.lastname = "Liedgren"; t1047.teacherID = "pld"; t1047.birthday = "680617"; teacherlist.Add(t1047);
            teacherclass t1048 = new teacherclass(); t1048.name = "Per Lindqvist"; t1048.firstname = "Per"; t1048.lastname = "Lindqvist"; t1048.teacherID = "pli"; t1048.birthday = "491010"; teacherlist.Add(t1048);
            teacherclass t1049 = new teacherclass(); t1049.name = "Patrik Larsson"; t1049.firstname = "Patrik"; t1049.lastname = "Larsson"; t1049.teacherID = "plo"; t1049.birthday = "700305"; teacherlist.Add(t1049);
            teacherclass t1050 = new teacherclass(); t1050.name = "Per Liljas"; t1050.firstname = "Per"; t1050.lastname = "Liljas"; t1050.teacherID = "pls"; t1050.birthday = "470427"; teacherlist.Add(t1050);
            teacherclass t1051 = new teacherclass(); t1051.name = "Peter Möller"; t1051.firstname = "Peter"; t1051.lastname = "Möller"; t1051.teacherID = "pmo"; t1051.birthday = "740717"; teacherlist.Add(t1051);
            teacherclass t1052 = new teacherclass(); t1052.name = "Patrik Mosveen"; t1052.firstname = "Patrik"; t1052.lastname = "Mosveen"; t1052.teacherID = "pmv"; t1052.birthday = "730219"; teacherlist.Add(t1052);
            teacherclass t1053 = new teacherclass(); t1053.name = "Per Nilsson"; t1053.firstname = "Per"; t1053.lastname = "Nilsson"; t1053.teacherID = "pni"; t1053.birthday = "520430"; teacherlist.Add(t1053);
            teacherclass t1054 = new teacherclass(); t1054.name = "Peter Nilsson"; t1054.firstname = "Peter"; t1054.lastname = "Nilsson"; t1054.teacherID = "pnl"; t1054.birthday = "701230"; teacherlist.Add(t1054);
            teacherclass t1055 = new teacherclass(); t1055.name = "Pernilla Nord"; t1055.firstname = "Pernilla"; t1055.lastname = "Nord"; t1055.teacherID = "pno"; t1055.birthday = "720111"; teacherlist.Add(t1055);
            teacherclass t1056 = new teacherclass(); t1056.name = "Per-Olov Bergström"; t1056.firstname = "Per-Olov"; t1056.lastname = "Bergström"; t1056.teacherID = "pob"; t1056.birthday = "520629"; teacherlist.Add(t1056);
            teacherclass t1057 = new teacherclass(); t1057.name = "Pouyan Pirouznia"; t1057.firstname = "Pouyan"; t1057.lastname = "Pirouznia"; t1057.teacherID = "ppi"; t1057.birthday = "850911"; teacherlist.Add(t1057);
            teacherclass t1058 = new teacherclass(); t1058.name = "Pascal Rebreyand"; t1058.firstname = "Pascal"; t1058.lastname = "Rebreyand"; t1058.teacherID = "prb"; t1058.birthday = "720306"; teacherlist.Add(t1058);
            teacherclass t1059 = new teacherclass(); t1059.name = "Peter Reinholdsson"; t1059.firstname = "Peter"; t1059.lastname = "Reinholdsson"; t1059.teacherID = "pre"; t1059.birthday = "610125"; teacherlist.Add(t1059);
            teacherclass t1060 = new teacherclass(); t1060.name = "Peter Romin"; t1060.firstname = "Peter"; t1060.lastname = "Romin"; t1060.teacherID = "prm"; t1060.birthday = "640804"; teacherlist.Add(t1060);
            teacherclass t1061 = new teacherclass(); t1061.name = "Pontus Sjöström"; t1061.firstname = "Pontus"; t1061.lastname = "Sjöström"; t1061.teacherID = "pss"; t1061.birthday = "470908"; teacherlist.Add(t1061);
            teacherclass t1062 = new teacherclass(); t1062.name = "Petra Wiklund"; t1062.firstname = "Petra"; t1062.lastname = "Wiklund"; t1062.teacherID = "psu"; t1062.birthday = "760118"; teacherlist.Add(t1062);
            teacherclass t1063 = new teacherclass(); t1063.name = "Kalfas Panagiotos-Theodoros"; t1063.firstname = "Kalfas"; t1063.lastname = "Panagiotos-Theodoros"; t1063.teacherID = "ptk"; t1063.birthday = "830207"; teacherlist.Add(t1063);
            teacherclass t1064 = new teacherclass(); t1064.name = "Peter Norberg"; t1064.firstname = "Peter"; t1064.lastname = "Norberg"; t1064.teacherID = "ptn"; t1064.birthday = "780131"; teacherlist.Add(t1064);
            teacherclass t1065 = new teacherclass(); t1065.name = "Per Wallén"; t1065.firstname = "Per"; t1065.lastname = "Wallén"; t1065.teacherID = "pwa"; t1065.birthday = "600604"; teacherlist.Add(t1065);
            teacherclass t1066 = new teacherclass(); t1066.name = "Pierre Vogel"; t1066.firstname = "Pierre"; t1066.lastname = "Vogel"; t1066.teacherID = "pvo"; t1066.birthday = "640913"; teacherlist.Add(t1066);
            teacherclass t1067 = new teacherclass(); t1067.name = "Ragnar Ahlström Söderling"; t1067.firstname = "Ragnar Ahlström"; t1067.lastname = "Söderling"; t1067.teacherID = "rah"; t1067.birthday = "460731"; teacherlist.Add(t1067);
            teacherclass t1068 = new teacherclass(); t1068.name = "Robert Andersson"; t1068.firstname = "Robert"; t1068.lastname = "Andersson"; t1068.teacherID = "ran"; t1068.birthday = "790517"; teacherlist.Add(t1068);
            teacherclass t1069 = new teacherclass(); t1069.name = "Rauno Noiva"; t1069.firstname = "Rauno"; t1069.lastname = "Noiva"; t1069.teacherID = "rauno"; t1069.birthday = "501001"; teacherlist.Add(t1069);
            teacherclass t1070 = new teacherclass(); t1070.name = "Rune Berglund"; t1070.firstname = "Rune"; t1070.lastname = "Berglund"; t1070.teacherID = "rbe"; t1070.birthday = "521016"; teacherlist.Add(t1070);
            teacherclass t1071 = new teacherclass(); t1071.name = "Rolf Björkman"; t1071.firstname = "Rolf"; t1071.lastname = "Björkman"; t1071.teacherID = "rbj"; t1071.birthday = "470310"; teacherlist.Add(t1071);
            teacherclass t1072 = new teacherclass(); t1072.name = "Richard Borg"; t1072.firstname = "Richard"; t1072.lastname = "Borg"; t1072.teacherID = "rbo"; t1072.birthday = "720403"; teacherlist.Add(t1072);
            teacherclass t1073 = new teacherclass(); t1073.name = "Reza Mortazavi"; t1073.firstname = "Reza"; t1073.lastname = "Mortazavi"; t1073.teacherID = "rem"; t1073.birthday = "650801"; teacherlist.Add(t1073);
            teacherclass t1074 = new teacherclass(); t1074.name = "Reneé Flacking"; t1074.firstname = "Reneé"; t1074.lastname = "Flacking"; t1074.teacherID = "rfl"; t1074.birthday = "640930"; teacherlist.Add(t1074);
            teacherclass t1075 = new teacherclass(); t1075.name = "Robin Fredriksson"; t1075.firstname = "Robin"; t1075.lastname = "Fredriksson"; t1075.teacherID = "rfr"; t1075.birthday = "890328"; teacherlist.Add(t1075);
            teacherclass t1076 = new teacherclass(); t1076.name = "Roland Granqvist"; t1076.firstname = "Roland"; t1076.lastname = "Granqvist"; t1076.teacherID = "rgr"; t1076.birthday = "431107"; teacherlist.Add(t1076);
            teacherclass t1077 = new teacherclass(); t1077.name = "Roger Hjortendahl"; t1077.firstname = "Roger"; t1077.lastname = "Hjortendahl"; t1077.teacherID = "rhj"; t1077.birthday = "600915"; teacherlist.Add(t1077);
            teacherclass t1078 = new teacherclass(); t1078.name = "Rolf Höckerlind"; t1078.firstname = "Rolf"; t1078.lastname = "Höckerlind"; t1078.teacherID = "rho"; t1078.birthday = "520203"; teacherlist.Add(t1078);
            teacherclass t1079 = new teacherclass(); t1079.name = "Richard Kohlström"; t1079.firstname = "Richard"; t1079.lastname = "Kohlström"; t1079.teacherID = "rik"; t1079.birthday = "460121"; teacherlist.Add(t1079);
            teacherclass t1080 = new teacherclass(); t1080.name = "Roger Johansson"; t1080.firstname = "Roger"; t1080.lastname = "Johansson"; t1080.teacherID = "rjo"; t1080.birthday = "660226"; teacherlist.Add(t1080);
            teacherclass t1081 = new teacherclass(); t1081.name = "Rita Magnusson"; t1081.firstname = "Rita"; t1081.lastname = "Magnusson"; t1081.teacherID = "rma"; t1081.birthday = "501026"; teacherlist.Add(t1081);
            teacherclass t1082 = new teacherclass(); t1082.name = "Roger Melin"; t1082.firstname = "Roger"; t1082.lastname = "Melin"; t1082.teacherID = "rme"; t1082.birthday = "650108"; teacherlist.Add(t1082);
            teacherclass t1083 = new teacherclass(); t1083.name = "Rolf Magnusson"; t1083.firstname = "Rolf"; t1083.lastname = "Magnusson"; t1083.teacherID = "rmg"; t1083.birthday = "440208"; teacherlist.Add(t1083);
            teacherclass t1084 = new teacherclass(); t1084.name = "Rebecca Magyar"; t1084.firstname = "Rebecca"; t1084.lastname = "Magyar"; t1084.teacherID = "rmy"; t1084.birthday = "940530"; teacherlist.Add(t1084);
            teacherclass t1085 = new teacherclass(); t1085.name = "Rina Maria Navarro Viadana"; t1085.firstname = "Rina Maria"; t1085.lastname = "Navarro Viadana"; t1085.teacherID = "rna"; t1085.birthday = "580622"; teacherlist.Add(t1085);
            teacherclass t1086 = new teacherclass(); t1086.name = "Rickard Norstedt"; t1086.firstname = "Rickard"; t1086.lastname = "Norstedt"; t1086.teacherID = "rno"; t1086.birthday = "890412"; teacherlist.Add(t1086);
            teacherclass t1087 = new teacherclass(); t1087.name = "Röde Nyström"; t1087.firstname = "Röde"; t1087.lastname = "Nyström"; t1087.teacherID = "rns"; t1087.birthday = "720511"; teacherlist.Add(t1087);
            teacherclass t1088 = new teacherclass(); t1088.name = "Roger Nyberg"; t1088.firstname = "Roger"; t1088.lastname = "Nyberg"; t1088.teacherID = "rny"; t1088.birthday = "691030"; teacherlist.Add(t1088);
            teacherclass t1089 = new teacherclass(); t1089.name = "Roland Hensby"; t1089.firstname = "Roland"; t1089.lastname = "Hensby"; t1089.teacherID = "rojo"; t1089.birthday = "680505"; teacherlist.Add(t1089);
            teacherclass t1090 = new teacherclass(); t1090.name = "Ragnar Olafsson"; t1090.firstname = "Ragnar"; t1090.lastname = "Olafsson"; t1090.teacherID = "rol"; t1090.birthday = "720829"; teacherlist.Add(t1090);
            teacherclass t1091 = new teacherclass(); t1091.name = "Röjd Ranne Andersson"; t1091.firstname = "Röjd Ranne"; t1091.lastname = "Andersson"; t1091.teacherID = "rra"; t1091.birthday = "510111"; teacherlist.Add(t1091);
            teacherclass t1092 = new teacherclass(); t1092.name = "Rieko Saito"; t1092.firstname = "Rieko"; t1092.lastname = "Saito"; t1092.teacherID = "rsa"; t1092.birthday = "801215"; teacherlist.Add(t1092);
            teacherclass t1093 = new teacherclass(); t1093.name = "Rolf Sjöberg"; t1093.firstname = "Rolf"; t1093.lastname = "Sjöberg"; t1093.teacherID = "rsj"; t1093.birthday = "670405"; teacherlist.Add(t1093);
            teacherclass t1094 = new teacherclass(); t1094.name = "Roger Säljö"; t1094.firstname = "Roger"; t1094.lastname = "Säljö"; t1094.teacherID = "rsl"; t1094.birthday = "480402"; teacherlist.Add(t1094);
            teacherclass t1095 = new teacherclass(); t1095.name = "Richard Stridbeck"; t1095.firstname = "Richard"; t1095.lastname = "Stridbeck"; t1095.teacherID = "rst"; t1095.birthday = "660728"; teacherlist.Add(t1095);
            teacherclass t1096 = new teacherclass(); t1096.name = "Robert Thorp"; t1096.firstname = "Robert"; t1096.lastname = "Thorp"; t1096.teacherID = "rth"; t1096.birthday = "760728"; teacherlist.Add(t1096);
            teacherclass t1097 = new teacherclass(); t1097.name = "Ramon Wåhlin"; t1097.firstname = "Ramon"; t1097.lastname = "Wåhlin"; t1097.teacherID = "rwa"; t1097.birthday = "481112"; teacherlist.Add(t1097);
            teacherclass t1098 = new teacherclass(); t1098.name = "Roger Westlund"; t1098.firstname = "Roger"; t1098.lastname = "Westlund"; t1098.teacherID = "rwe"; t1098.birthday = "671116"; teacherlist.Add(t1098);
            teacherclass t1099 = new teacherclass(); t1099.name = "Somayeh Aghanavesi"; t1099.firstname = "Somayeh"; t1099.lastname = "Aghanavesi"; t1099.teacherID = "saa"; t1099.birthday = "810830"; teacherlist.Add(t1099);
            teacherclass t1100 = new teacherclass(); t1100.name = "Solveig Ahlin"; t1100.firstname = "Solveig"; t1100.lastname = "Ahlin"; t1100.teacherID = "sah"; t1100.birthday = "540615"; teacherlist.Add(t1100);
            teacherclass t1101 = new teacherclass(); t1101.name = "Sofie Alroth"; t1101.firstname = "Sofie"; t1101.lastname = "Alroth"; t1101.teacherID = "sal"; t1101.birthday = "661006"; teacherlist.Add(t1101);
            teacherclass t1102 = new teacherclass(); t1102.name = "Staffan Andersson"; t1102.firstname = "Staffan"; t1102.lastname = "Andersson"; t1102.teacherID = "san"; t1102.birthday = "760421"; teacherlist.Add(t1102);
            teacherclass t1103 = new teacherclass(); t1103.name = "Suzanne Andersson"; t1103.firstname = "Suzanne"; t1103.lastname = "Andersson"; t1103.teacherID = "sao"; t1103.birthday = "720907"; teacherlist.Add(t1103);
            teacherclass t1104 = new teacherclass(); t1104.name = "Susanne Antell"; t1104.firstname = "Susanne"; t1104.lastname = "Antell"; t1104.teacherID = "sat"; t1104.birthday = "640205"; teacherlist.Add(t1104);
            teacherclass t1105 = new teacherclass(); t1105.name = "Sanna Bäcke"; t1105.firstname = "Sanna"; t1105.lastname = "Bäcke"; t1105.teacherID = "sba"; t1105.birthday = "891105"; teacherlist.Add(t1105);
            teacherclass t1106 = new teacherclass(); t1106.name = "Sara Bengts"; t1106.firstname = "Sara"; t1106.lastname = "Bengts"; t1106.teacherID = "sbg"; t1106.birthday = "960622"; teacherlist.Add(t1106);
            teacherclass t1107 = new teacherclass(); t1107.name = "Stefan Björnlund"; t1107.firstname = "Stefan"; t1107.lastname = "Björnlund"; t1107.teacherID = "sbj"; t1107.birthday = "810725"; teacherlist.Add(t1107);
            teacherclass t1108 = new teacherclass(); t1108.name = "Sten Bergman"; t1108.firstname = "Sten"; t1108.lastname = "Bergman"; t1108.teacherID = "sbm"; t1108.birthday = "350802"; teacherlist.Add(t1108);
            teacherclass t1109 = new teacherclass(); t1109.name = "Solveig Böhn"; t1109.firstname = "Solveig"; t1109.lastname = "Böhn"; t1109.teacherID = "sbn"; t1109.birthday = "450505"; teacherlist.Add(t1109);
            teacherclass t1110 = new teacherclass(); t1110.name = "Sofia Brorsson"; t1110.firstname = "Sofia"; t1110.lastname = "Brorsson"; t1110.teacherID = "sbo"; t1110.birthday = "731208"; teacherlist.Add(t1110);
            teacherclass t1111 = new teacherclass(); t1111.name = "Sarah Ramsay"; t1111.firstname = "Sarah"; t1111.lastname = "Ramsay"; t1111.teacherID = "sbr"; t1111.birthday = "670721"; teacherlist.Add(t1111);
            teacherclass t1112 = new teacherclass(); t1112.name = "Stefan Cassel"; t1112.firstname = "Stefan"; t1112.lastname = "Cassel"; t1112.teacherID = "sca"; t1112.birthday = "620207"; teacherlist.Add(t1112);
            teacherclass t1113 = new teacherclass(); t1113.name = "Susanne Corrigox"; t1113.firstname = "Susanne"; t1113.lastname = "Corrigox"; t1113.teacherID = "sco"; t1113.birthday = "601117"; teacherlist.Add(t1113);
            teacherclass t1114 = new teacherclass(); t1114.name = "Stefan Eriksson"; t1114.firstname = "Stefan"; t1114.lastname = "Eriksson"; t1114.teacherID = "sei"; t1114.birthday = "661114"; teacherlist.Add(t1114);
            teacherclass t1115 = new teacherclass(); t1115.name = "Sven E-G Eklund"; t1115.firstname = "Sven E-G"; t1115.lastname = "Eklund"; t1115.teacherID = "sek"; t1115.birthday = "640302"; teacherlist.Add(t1115);
            teacherclass t1116 = new teacherclass(); t1116.name = "Sofia Ellström"; t1116.firstname = "Sofia"; t1116.lastname = "Ellström"; t1116.teacherID = "sel"; t1116.birthday = "930227"; teacherlist.Add(t1116);
            teacherclass t1117 = new teacherclass(); t1117.name = "Susanne Erkes"; t1117.firstname = "Susanne"; t1117.lastname = "Erkes"; t1117.teacherID = "ser"; t1117.birthday = "570806"; teacherlist.Add(t1117);
            teacherclass t1118 = new teacherclass(); t1118.name = "Sara Klingberg Fridner"; t1118.firstname = "Sara"; t1118.lastname = "Klingberg Fridner"; t1118.teacherID = "sfk"; t1118.birthday = "741222"; teacherlist.Add(t1118);
            teacherclass t1119 = new teacherclass(); t1119.name = "Susanna Gahnshag"; t1119.firstname = "Susanna"; t1119.lastname = "Gahnshag"; t1119.teacherID = "sga"; t1119.birthday = "720807"; teacherlist.Add(t1119);
            teacherclass t1120 = new teacherclass(); t1120.name = "Susanna Cassel Heldt"; t1120.firstname = "Susanna Cassel"; t1120.lastname = "Heldt"; t1120.teacherID = "shc"; t1120.birthday = "720605"; teacherlist.Add(t1120);
            teacherclass t1121 = new teacherclass(); t1121.name = "Sofi Dougherty H."; t1121.firstname = "Sofi"; t1121.lastname = "Dougherty H."; t1121.teacherID = "shd"; t1121.birthday = "670512"; teacherlist.Add(t1121);
            teacherclass t1122 = new teacherclass(); t1122.name = "Sven Hansell"; t1122.firstname = "Sven"; t1122.lastname = "Hansell"; t1122.teacherID = "she"; t1122.birthday = "490416"; teacherlist.Add(t1122);
            teacherclass t1123 = new teacherclass(); t1123.name = "Samira Hennius"; t1123.firstname = "Samira"; t1123.lastname = "Hennius"; t1123.teacherID = "shi"; t1123.birthday = "720506"; teacherlist.Add(t1123);
            teacherclass t1124 = new teacherclass(); t1124.name = "Steven Hunter-Lindqvist"; t1124.firstname = "Steven"; t1124.lastname = "Hunter-Lindqvist"; t1124.teacherID = "shl"; t1124.birthday = "591022"; teacherlist.Add(t1124);
            teacherclass t1125 = new teacherclass(); t1125.name = "Solveig Hannersjö"; t1125.firstname = "Solveig"; t1125.lastname = "Hannersjö"; t1125.teacherID = "shn"; t1125.birthday = "420325"; teacherlist.Add(t1125);
            teacherclass t1126 = new teacherclass(); t1126.name = "Sören Högberg"; t1126.firstname = "Sören"; t1126.lastname = "Högberg"; t1126.teacherID = "sho"; t1126.birthday = "560913"; teacherlist.Add(t1126);
            teacherclass t1127 = new teacherclass(); t1127.name = "Sofia Hansson"; t1127.firstname = "Sofia"; t1127.lastname = "Hansson"; t1127.teacherID = "shs"; t1127.birthday = "790131"; teacherlist.Add(t1127);
            teacherclass t1128 = new teacherclass(); t1128.name = "Sara Irisdotter Aldenmyr"; t1128.firstname = "Sara"; t1128.lastname = "Irisdotter Aldenmyr"; t1128.teacherID = "sia"; t1128.birthday = "761201"; teacherlist.Add(t1128);
            teacherclass t1129 = new teacherclass(); t1129.name = "Sylvia Ingemarsdotter"; t1129.firstname = "Sylvia"; t1129.lastname = "Ingemarsdotter"; t1129.teacherID = "sin"; t1129.birthday = "490510"; teacherlist.Add(t1129);
            teacherclass t1130 = new teacherclass(); t1130.name = "Sven Israelsson"; t1130.firstname = "Sven"; t1130.lastname = "Israelsson"; t1130.teacherID = "sir"; t1130.birthday = "400612"; teacherlist.Add(t1130);
            teacherclass t1131 = new teacherclass(); t1131.name = "Sten-Inge Sörslätt"; t1131.firstname = "Sten-Inge"; t1131.lastname = "Sörslätt"; t1131.teacherID = "sis"; t1131.birthday = "720218"; teacherlist.Add(t1131);
            teacherclass t1132 = new teacherclass(); t1132.name = "Sverker Johansson"; t1132.firstname = "Sverker"; t1132.lastname = "Johansson"; t1132.teacherID = "sja"; t1132.birthday = "610526"; teacherlist.Add(t1132);
            teacherclass t1133 = new teacherclass(); t1133.name = "Siw Jenssen"; t1133.firstname = "Siw"; t1133.lastname = "Jenssen"; t1133.teacherID = "sje"; t1133.birthday = "551215"; teacherlist.Add(t1133);
            teacherclass t1134 = new teacherclass(); t1134.name = "Stina Jeffner"; t1134.firstname = "Stina"; t1134.lastname = "Jeffner"; t1134.teacherID = "sjf"; t1134.birthday = "620203"; teacherlist.Add(t1134);
            teacherclass t1135 = new teacherclass(); t1135.name = "Sören Johansson"; t1135.firstname = "Sören"; t1135.lastname = "Johansson"; t1135.teacherID = "sjh"; t1135.birthday = "620719"; teacherlist.Add(t1135);
            teacherclass t1136 = new teacherclass(); t1136.name = "Stefan Jonsson"; t1136.firstname = "Stefan"; t1136.lastname = "Jonsson"; t1136.teacherID = "sjn"; t1136.birthday = "650519"; teacherlist.Add(t1136);
            teacherclass t1137 = new teacherclass(); t1137.name = "Sara Kalucza"; t1137.firstname = "Sara"; t1137.lastname = "Kalucza"; t1137.teacherID = "skl"; t1137.birthday = "870516"; teacherlist.Add(t1137);
            teacherclass t1138 = new teacherclass(); t1138.name = "Susanne Koistinen"; t1138.firstname = "Susanne"; t1138.lastname = "Koistinen"; t1138.teacherID = "sko"; t1138.birthday = "730825"; teacherlist.Add(t1138);
            teacherclass t1139 = new teacherclass(); t1139.name = "Sara Karlsson"; t1139.firstname = "Sara"; t1139.lastname = "Karlsson"; t1139.teacherID = "skr"; t1139.birthday = "820302"; teacherlist.Add(t1139);
            teacherclass t1140 = new teacherclass(); t1140.name = "Stefan Larsson"; t1140.firstname = "Stefan"; t1140.lastname = "Larsson"; t1140.teacherID = "slr"; t1140.birthday = "540927"; teacherlist.Add(t1140);
            teacherclass t1141 = new teacherclass(); t1141.name = "Sinikka Laurila"; t1141.firstname = "Sinikka"; t1141.lastname = "Laurila"; t1141.teacherID = "slu"; t1141.birthday = "580108"; teacherlist.Add(t1141);
            teacherclass t1142 = new teacherclass(); t1142.name = "Sonja Björnlund"; t1142.firstname = "Sonja"; t1142.lastname = "Björnlund"; t1142.teacherID = "sma"; t1142.birthday = "600331"; teacherlist.Add(t1142);
            teacherclass t1143 = new teacherclass(); t1143.name = "Sefija Melkic Larsson"; t1143.firstname = "Sefija"; t1143.lastname = "Melkic Larsson"; t1143.teacherID = "smk"; t1143.birthday = "660905"; teacherlist.Add(t1143);
            teacherclass t1144 = new teacherclass(); t1144.name = "Solveig Malmsten"; t1144.firstname = "Solveig"; t1144.lastname = "Malmsten"; t1144.teacherID = "sml"; t1144.birthday = "751218"; teacherlist.Add(t1144);
            teacherclass t1145 = new teacherclass(); t1145.name = "Sofia Mogård"; t1145.firstname = "Sofia"; t1145.lastname = "Mogård"; t1145.teacherID = "smo"; t1145.birthday = "740814"; teacherlist.Add(t1145);
            teacherclass t1146 = new teacherclass(); t1146.name = "Mayumi Senoo"; t1146.firstname = "Mayumi"; t1146.lastname = "Senoo"; t1146.teacherID = "smy"; t1146.birthday = "731103"; teacherlist.Add(t1146);
            teacherclass t1147 = new teacherclass(); t1147.name = "Sara Nordlund"; t1147.firstname = "Sara"; t1147.lastname = "Nordlund"; t1147.teacherID = "snd"; t1147.birthday = "940220"; teacherlist.Add(t1147);
            teacherclass t1148 = new teacherclass(); t1148.name = "Sara Nittve"; t1148.firstname = "Sara"; t1148.lastname = "Nittve"; t1148.teacherID = "sni"; t1148.birthday = "750304"; teacherlist.Add(t1148);
            teacherclass t1149 = new teacherclass(); t1149.name = "Sofia Norlander"; t1149.firstname = "Sofia"; t1149.lastname = "Norlander"; t1149.teacherID = "snl"; t1149.birthday = "771018"; teacherlist.Add(t1149);
            teacherclass t1150 = new teacherclass(); t1150.name = "Susanna Nordin"; t1150.firstname = "Susanna"; t1150.lastname = "Nordin"; t1150.teacherID = "snr"; t1150.birthday = "670203"; teacherlist.Add(t1150);
            teacherclass t1151 = new teacherclass(); t1151.name = "Sanja Nilsson"; t1151.firstname = "Sanja"; t1151.lastname = "Nilsson"; t1151.teacherID = "sns"; t1151.birthday = "780507"; teacherlist.Add(t1151);
            teacherclass t1152 = new teacherclass(); t1152.name = "Sven-Olov Daunfeldt"; t1152.firstname = "Sven-Olov"; t1152.lastname = "Daunfeldt"; t1152.teacherID = "sod"; t1152.birthday = "700914"; teacherlist.Add(t1152);
            teacherclass t1153 = new teacherclass(); t1153.name = "Totte Mattsson"; t1153.firstname = "Totte"; t1153.lastname = "Mattsson"; t1153.teacherID = "som"; t1153.birthday = "550719"; teacherlist.Add(t1153);
            teacherclass t1154 = new teacherclass(); t1154.name = "Solveig Sundin"; t1154.firstname = "Solveig"; t1154.lastname = "Sundin"; t1154.teacherID = "sosu"; t1154.birthday = "430720"; teacherlist.Add(t1154);
            teacherclass t1155 = new teacherclass(); t1155.name = "Sara Otterskog"; t1155.firstname = "Sara"; t1155.lastname = "Otterskog"; t1155.teacherID = "sot"; t1155.birthday = "800423"; teacherlist.Add(t1155);
            teacherclass t1156 = new teacherclass(); t1156.name = "Stefan Pettersson"; t1156.firstname = "Stefan"; t1156.lastname = "Pettersson"; t1156.teacherID = "spe"; t1156.birthday = "850402"; teacherlist.Add(t1156);
            teacherclass t1157 = new teacherclass(); t1157.name = "Stefano Poppi"; t1157.firstname = "Stefano"; t1157.lastname = "Poppi"; t1157.teacherID = "spo"; t1157.birthday = "820320"; teacherlist.Add(t1157);
            teacherclass t1158 = new teacherclass(); t1158.name = "Sofia Pettersson"; t1158.firstname = "Sofia"; t1158.lastname = "Pettersson"; t1158.teacherID = "spt"; t1158.birthday = "850912"; teacherlist.Add(t1158);
            teacherclass t1159 = new teacherclass(); t1159.name = "Sofia Pulkkinen"; t1159.firstname = "Sofia"; t1159.lastname = "Pulkkinen"; t1159.teacherID = "spu"; t1159.birthday = "920115"; teacherlist.Add(t1159);
            teacherclass t1160 = new teacherclass(); t1160.name = "Susanne Römsing"; t1160.firstname = "Susanne"; t1160.lastname = "Römsing"; t1160.teacherID = "srm"; t1160.birthday = "670516"; teacherlist.Add(t1160);
            teacherclass t1161 = new teacherclass(); t1161.name = "Stefan Rodheim"; t1161.firstname = "Stefan"; t1161.lastname = "Rodheim"; t1161.teacherID = "sro"; t1161.birthday = "660911"; teacherlist.Add(t1161);
            teacherclass t1162 = new teacherclass(); t1162.name = "Susanne Rosén"; t1162.firstname = "Susanne"; t1162.lastname = "Rosén"; t1162.teacherID = "srs"; t1162.birthday = "561027"; teacherlist.Add(t1162);
            teacherclass t1163 = new teacherclass(); t1163.name = "Steven Saxonberg"; t1163.firstname = "Steven"; t1163.lastname = "Saxonberg"; t1163.teacherID = "ssa"; t1163.birthday = "610509"; teacherlist.Add(t1163);
            teacherclass t1164 = new teacherclass(); t1164.name = "Sara Sundstedt"; t1164.firstname = "Sara"; t1164.lastname = "Sundstedt"; t1164.teacherID = "ssd"; t1164.birthday = "750703"; teacherlist.Add(t1164);
            teacherclass t1165 = new teacherclass(); t1165.name = "Sigrid Saveljeff"; t1165.firstname = "Sigrid"; t1165.lastname = "Saveljeff"; t1165.teacherID = "ssf"; t1165.birthday = "761110"; teacherlist.Add(t1165);
            teacherclass t1166 = new teacherclass(); t1166.name = "Sara Saketi"; t1166.firstname = "Sara"; t1166.lastname = "Saketi"; t1166.teacherID = "ssi"; t1166.birthday = "800425"; teacherlist.Add(t1166);
            teacherclass t1167 = new teacherclass(); t1167.name = "Stefan Sjöholm"; t1167.firstname = "Stefan"; t1167.lastname = "Sjöholm"; t1167.teacherID = "ssj"; t1167.birthday = "660205"; teacherlist.Add(t1167);
            teacherclass t1168 = new teacherclass(); t1168.name = "Sten Sundin"; t1168.firstname = "Sten"; t1168.lastname = "Sundin"; t1168.teacherID = "ssn"; t1168.birthday = "510904"; teacherlist.Add(t1168);
            teacherclass t1169 = new teacherclass(); t1169.name = "Sara Sjögren"; t1169.firstname = "Sara"; t1169.lastname = "Sjögren"; t1169.teacherID = "sso"; t1169.birthday = "790719"; teacherlist.Add(t1169);
            teacherclass t1170 = new teacherclass(); t1170.name = "Satu Sundström"; t1170.firstname = "Satu"; t1170.lastname = "Sundström"; t1170.teacherID = "ssu"; t1170.birthday = "560825"; teacherlist.Add(t1170);
            teacherclass t1171 = new teacherclass(); t1171.name = "Sabina Tabacaru"; t1171.firstname = "Sabina"; t1171.lastname = "Tabacaru"; t1171.teacherID = "sta"; t1171.birthday = "860375"; teacherlist.Add(t1171);
            teacherclass t1172 = new teacherclass(); t1172.name = "Soraya Tharani"; t1172.firstname = "Soraya"; t1172.lastname = "Tharani"; t1172.teacherID = "sth"; t1172.birthday = "680308"; teacherlist.Add(t1172);
            teacherclass t1173 = new teacherclass(); t1173.name = "Tomas Johansson"; t1173.firstname = "Tomas"; t1173.lastname = "Johansson"; t1173.teacherID = "stj"; t1173.birthday = "660215"; teacherlist.Add(t1173);
            teacherclass t1174 = new teacherclass(); t1174.name = "Sverre Wide"; t1174.firstname = "Sverre"; t1174.lastname = "Wide"; t1174.teacherID = "swi"; t1174.birthday = "731204"; teacherlist.Add(t1174);
            teacherclass t1175 = new teacherclass(); t1175.name = "Sofia Walter"; t1175.firstname = "Sofia"; t1175.lastname = "Walter"; t1175.teacherID = "swl"; t1175.birthday = "700422"; teacherlist.Add(t1175);
            teacherclass t1176 = new teacherclass(); t1176.name = "Sara Wolff"; t1176.firstname = "Sara"; t1176.lastname = "Wolff"; t1176.teacherID = "swo"; t1176.birthday = "720803"; teacherlist.Add(t1176);
            teacherclass t1177 = new teacherclass(); t1177.name = "Siril Yella"; t1177.firstname = "Siril"; t1177.lastname = "Yella"; t1177.teacherID = "sye"; t1177.birthday = "790710"; teacherlist.Add(t1177);
            teacherclass t1178 = new teacherclass(); t1178.name = "Sophie Yvert Hammon"; t1178.firstname = "Sophie"; t1178.lastname = "Yvert Hammon"; t1178.teacherID = "syh"; t1178.birthday = "730703"; teacherlist.Add(t1178);
            teacherclass t1179 = new teacherclass(); t1179.name = "Sana Zubair Khan"; t1179.firstname = "Sana Zubair"; t1179.lastname = "Khan"; t1179.teacherID = "szk"; t1179.birthday = "820531"; teacherlist.Add(t1179);
            teacherclass t1180 = new teacherclass(); t1180.name = "Torbjörn Allard"; t1180.firstname = "Torbjörn"; t1180.lastname = "Allard"; t1180.teacherID = "taa"; t1180.birthday = "600309"; teacherlist.Add(t1180);
            teacherclass t1181 = new teacherclass(); t1181.name = "Tarja Alatalo"; t1181.firstname = "Tarja"; t1181.lastname = "Alatalo"; t1181.teacherID = "tao"; t1181.birthday = "611105"; teacherlist.Add(t1181);
            teacherclass t1182 = new teacherclass(); t1182.name = "Tomas Axelson"; t1182.firstname = "Tomas"; t1182.lastname = "Axelson"; t1182.teacherID = "tax"; t1182.birthday = "600606"; teacherlist.Add(t1182);
            teacherclass t1183 = new teacherclass(); t1183.name = "Torsten Blomkvist"; t1183.firstname = "Torsten"; t1183.lastname = "Blomkvist"; t1183.teacherID = "tbm"; t1183.birthday = "721005"; teacherlist.Add(t1183);
            teacherclass t1184 = new teacherclass(); t1184.name = "Tammy Blom"; t1184.firstname = "Tammy"; t1184.lastname = "Blom"; t1184.teacherID = "tbo"; t1184.birthday = "500627"; teacherlist.Add(t1184);
            teacherclass t1185 = new teacherclass(); t1185.name = "Toivo Burlin"; t1185.firstname = "Toivo"; t1185.lastname = "Burlin"; t1185.teacherID = "tbu"; t1185.birthday = "720510"; teacherlist.Add(t1185);
            teacherclass t1186 = new teacherclass(); t1186.name = "Tomas Carlsson"; t1186.firstname = "Tomas"; t1186.lastname = "Carlsson"; t1186.teacherID = "tca"; t1186.birthday = "690701"; teacherlist.Add(t1186);
            teacherclass t1187 = new teacherclass(); t1187.name = "Torbjörn Danielsson"; t1187.firstname = "Torbjörn"; t1187.lastname = "Danielsson"; t1187.teacherID = "tdn"; t1187.birthday = "501224"; teacherlist.Add(t1187);
            teacherclass t1188 = new teacherclass(); t1188.name = "Torbjörn Eriksson"; t1188.firstname = "Torbjörn"; t1188.lastname = "Eriksson"; t1188.teacherID = "tes"; t1188.birthday = "710417"; teacherlist.Add(t1188);
            teacherclass t1189 = new teacherclass(); t1189.name = "Thomas Florén"; t1189.firstname = "Thomas"; t1189.lastname = "Florén"; t1189.teacherID = "tfl"; t1189.birthday = "690829"; teacherlist.Add(t1189);
            teacherclass t1190 = new teacherclass(); t1190.name = "Therese Granström"; t1190.firstname = "Therese"; t1190.lastname = "Granström"; t1190.teacherID = "tga"; t1190.birthday = "691020"; teacherlist.Add(t1190);
            teacherclass t1191 = new teacherclass(); t1191.name = "Torbjörn Gustafsson"; t1191.firstname = "Torbjörn"; t1191.lastname = "Gustafsson"; t1191.teacherID = "tgf"; t1191.birthday = "741025"; teacherlist.Add(t1191);
            teacherclass t1192 = new teacherclass(); t1192.name = "Tomas Gullberg"; t1192.firstname = "Tomas"; t1192.lastname = "Gullberg"; t1192.teacherID = "tgu"; t1192.birthday = "600220"; teacherlist.Add(t1192);
            teacherclass t1193 = new teacherclass(); t1193.name = "Terje Hedström"; t1193.firstname = "Terje"; t1193.lastname = "Hedström"; t1193.teacherID = "thd"; t1193.birthday = "590330"; teacherlist.Add(t1193);
            teacherclass t1194 = new teacherclass(); t1194.name = "Tobias Heldt"; t1194.firstname = "Tobias"; t1194.lastname = "Heldt"; t1194.teacherID = "the"; t1194.birthday = "721207"; teacherlist.Add(t1194);
            teacherclass t1195 = new teacherclass(); t1195.name = "Therese Herkules"; t1195.firstname = "Therese"; t1195.lastname = "Herkules"; t1195.teacherID = "thr"; t1195.birthday = "710415"; teacherlist.Add(t1195);
            teacherclass t1196 = new teacherclass(); t1196.name = "Torsten Hylén"; t1196.firstname = "Torsten"; t1196.lastname = "Hylén"; t1196.teacherID = "thy"; t1196.birthday = "560815"; teacherlist.Add(t1196);
            teacherclass t1197 = new teacherclass(); t1197.name = "Tina Wik"; t1197.firstname = "Tina"; t1197.lastname = "Wik"; t1197.teacherID = "tiw"; t1197.birthday = "551025"; teacherlist.Add(t1197);
            teacherclass t1198 = new teacherclass(); t1198.name = "Tao Yang"; t1198.firstname = "Tao"; t1198.lastname = "Yang"; t1198.teacherID = "tjn"; t1198.birthday = "660617"; teacherlist.Add(t1198);
            teacherclass t1199 = new teacherclass(); t1199.name = "Tanja Jörgensen"; t1199.firstname = "Tanja"; t1199.lastname = "Jörgensen"; t1199.teacherID = "tjr"; t1199.birthday = "690916"; teacherlist.Add(t1199);
            teacherclass t1200 = new teacherclass(); t1200.name = "Threse Jonsson"; t1200.firstname = "Threse"; t1200.lastname = "Jonsson"; t1200.teacherID = "tjs"; t1200.birthday = "770404"; teacherlist.Add(t1200);
            teacherclass t1201 = new teacherclass(); t1201.name = "Taha Khan"; t1201.firstname = "Taha"; t1201.lastname = "Khan"; t1201.teacherID = "tkh"; t1201.birthday = "831101"; teacherlist.Add(t1201);
            teacherclass t1202 = new teacherclass(); t1202.name = "Chandra Tara Kandpal"; t1202.firstname = "Chandra Tara"; t1202.lastname = "Kandpal"; t1202.teacherID = "tkn"; t1202.birthday = "560873"; teacherlist.Add(t1202);
            teacherclass t1203 = new teacherclass(); t1203.name = "Thomas Kvist"; t1203.firstname = "Thomas"; t1203.lastname = "Kvist"; t1203.teacherID = "tkv"; t1203.birthday = "560710"; teacherlist.Add(t1203);
            teacherclass t1204 = new teacherclass(); t1204.name = "Tobias Lundberg"; t1204.firstname = "Tobias"; t1204.lastname = "Lundberg"; t1204.teacherID = "tld"; t1204.birthday = "710128"; teacherlist.Add(t1204);
            teacherclass t1205 = new teacherclass(); t1205.name = "Thomas Lüthi"; t1205.firstname = "Thomas"; t1205.lastname = "Lüthi"; t1205.teacherID = "tlt"; t1205.birthday = "581123"; teacherlist.Add(t1205);
            teacherclass t1206 = new teacherclass(); t1206.name = "Thomas Nygren"; t1206.firstname = "Thomas"; t1206.lastname = "Nygren"; t1206.teacherID = "tny"; t1206.birthday = "720128"; teacherlist.Add(t1206);
            teacherclass t1207 = new teacherclass(); t1207.name = "Torbjörn Olofsson"; t1207.firstname = "Torbjörn"; t1207.lastname = "Olofsson"; t1207.teacherID = "tol"; t1207.birthday = "730531"; teacherlist.Add(t1207);
            teacherclass t1208 = new teacherclass(); t1208.name = "Thomas Olofsson"; t1208.firstname = "Thomas"; t1208.lastname = "Olofsson"; t1208.teacherID = "too"; t1208.birthday = "680612"; teacherlist.Add(t1208);
            teacherclass t1209 = new teacherclass(); t1209.name = "Tomas Persson"; t1209.firstname = "Tomas"; t1209.lastname = "Persson"; t1209.teacherID = "tpe"; t1209.birthday = "720506"; teacherlist.Add(t1209);
            teacherclass t1210 = new teacherclass(); t1210.name = "Tatu Räsänen"; t1210.firstname = "Tatu"; t1210.lastname = "Räsänen"; t1210.teacherID = "tra"; t1210.birthday = "660304"; teacherlist.Add(t1210);
            teacherclass t1211 = new teacherclass(); t1211.name = "Therese Rodin"; t1211.firstname = "Therese"; t1211.lastname = "Rodin"; t1211.teacherID = "trd"; t1211.birthday = "710205"; teacherlist.Add(t1211);
            teacherclass t1212 = new teacherclass(); t1212.name = "Tobias Rudholm Feldrich"; t1212.firstname = "Tobias"; t1212.lastname = "Rudholm Feldrich"; t1212.teacherID = "trf"; t1212.birthday = "760804"; teacherlist.Add(t1212);
            teacherclass t1213 = new teacherclass(); t1213.name = "Tapio Raunio"; t1213.firstname = "Tapio"; t1213.lastname = "Raunio"; t1213.teacherID = "trn"; t1213.birthday = "691162"; teacherlist.Add(t1213);
            teacherclass t1214 = new teacherclass(); t1214.name = "Thomas Sedelius"; t1214.firstname = "Thomas"; t1214.lastname = "Sedelius"; t1214.teacherID = "tse"; t1214.birthday = "760326"; teacherlist.Add(t1214);
            teacherclass t1215 = new teacherclass(); t1215.name = "Tony Svensson"; t1215.firstname = "Tony"; t1215.lastname = "Svensson"; t1215.teacherID = "tsn"; t1215.birthday = "740711"; teacherlist.Add(t1215);
            teacherclass t1216 = new teacherclass(); t1216.name = "Thorbjörn Swenberg"; t1216.firstname = "Thorbjörn"; t1216.lastname = "Swenberg"; t1216.teacherID = "tsw"; t1216.birthday = "680508"; teacherlist.Add(t1216);
            teacherclass t1217 = new teacherclass(); t1217.name = "Tommy Tjernström"; t1217.firstname = "Tommy"; t1217.lastname = "Tjernström"; t1217.teacherID = "ttj"; t1217.birthday = "620303"; teacherlist.Add(t1217);
            teacherclass t1218 = new teacherclass(); t1218.name = "Thomas Thydén"; t1218.firstname = "Thomas"; t1218.lastname = "Thydén"; t1218.teacherID = "tty"; t1218.birthday = "461014"; teacherlist.Add(t1218);
            teacherclass t1219 = new teacherclass(); t1219.name = "Tony Wiklund"; t1219.firstname = "Tony"; t1219.lastname = "Wiklund"; t1219.teacherID = "twk"; t1219.birthday = "750821"; teacherlist.Add(t1219);
            teacherclass t1220 = new teacherclass(); t1220.name = "Ulla Abrahamsen"; t1220.firstname = "Ulla"; t1220.lastname = "Abrahamsen"; t1220.teacherID = "uab"; t1220.birthday = "670419"; teacherlist.Add(t1220);
            teacherclass t1221 = new teacherclass(); t1221.name = "Ulrika Åkerlund"; t1221.firstname = "Ulrika"; t1221.lastname = "Åkerlund"; t1221.teacherID = "uak"; t1221.birthday = "831010"; teacherlist.Add(t1221);
            teacherclass t1222 = new teacherclass(); t1222.name = "Ulla Allard"; t1222.firstname = "Ulla"; t1222.lastname = "Allard"; t1222.teacherID = "ual"; t1222.birthday = "540922"; teacherlist.Add(t1222);
            teacherclass t1223 = new teacherclass(); t1223.name = "Ulrika Wissa Artursson"; t1223.firstname = "Ulrika"; t1223.lastname = "Wissa Artursson"; t1223.teacherID = "uaw"; t1223.birthday = "711024"; teacherlist.Add(t1223);
            teacherclass t1224 = new teacherclass(); t1224.name = "Ulrika Boman"; t1224.firstname = "Ulrika"; t1224.lastname = "Boman"; t1224.teacherID = "ubo"; t1224.birthday = "870825"; teacherlist.Add(t1224);
            teacherclass t1225 = new teacherclass(); t1225.name = "Ulf Bexell"; t1225.firstname = "Ulf"; t1225.lastname = "Bexell"; t1225.teacherID = "ubx"; t1225.birthday = "660907"; teacherlist.Add(t1225);
            teacherclass t1226 = new teacherclass(); t1226.name = "Ulrika Byrskog"; t1226.firstname = "Ulrika"; t1226.lastname = "Byrskog"; t1226.teacherID = "uby"; t1226.birthday = "700216"; teacherlist.Add(t1226);
            teacherclass t1227 = new teacherclass(); t1227.name = "Urban Claesson"; t1227.firstname = "Urban"; t1227.lastname = "Claesson"; t1227.teacherID = "ucl"; t1227.birthday = "690731"; teacherlist.Add(t1227);
            teacherclass t1228 = new teacherclass(); t1228.name = "Ulrika Eriksson"; t1228.firstname = "Ulrika"; t1228.lastname = "Eriksson"; t1228.teacherID = "uer"; t1228.birthday = "640626"; teacherlist.Add(t1228);
            teacherclass t1229 = new teacherclass(); t1229.name = "Ulrika Förberg"; t1229.firstname = "Ulrika"; t1229.lastname = "Förberg"; t1229.teacherID = "ufo"; t1229.birthday = "800724"; teacherlist.Add(t1229);
            teacherclass t1230 = new teacherclass(); t1230.name = "Ulrika Gabrielsson"; t1230.firstname = "Ulrika"; t1230.lastname = "Gabrielsson"; t1230.teacherID = "uga"; t1230.birthday = "740421"; teacherlist.Add(t1230);
            teacherclass t1231 = new teacherclass(); t1231.name = "Ulla Gunnarsson"; t1231.firstname = "Ulla"; t1231.lastname = "Gunnarsson"; t1231.teacherID = "ugu"; t1231.birthday = "590514"; teacherlist.Add(t1231);
            teacherclass t1232 = new teacherclass(); t1232.name = "Ulf Karlmats"; t1232.firstname = "Ulf"; t1232.lastname = "Karlmats"; t1232.teacherID = "uka"; t1232.birthday = "600218"; teacherlist.Add(t1232);
            teacherclass t1233 = new teacherclass(); t1233.name = "Ulf Kassfeldt"; t1233.firstname = "Ulf"; t1233.lastname = "Kassfeldt"; t1233.teacherID = "ukf"; t1233.birthday = "700301"; teacherlist.Add(t1233);
            teacherclass t1234 = new teacherclass(); t1234.name = "Ulla-Karin Schön"; t1234.firstname = "Ulla-Karin"; t1234.lastname = "Schön"; t1234.teacherID = "uks"; t1234.birthday = "700313"; teacherlist.Add(t1234);
            teacherclass t1235 = new teacherclass(); t1235.name = "Ahlen Ulla-Carin Lindström"; t1235.firstname = "Ahlen Ulla-Carin"; t1235.lastname = "Lindström"; t1235.teacherID = "ula"; t1235.birthday = "690408"; teacherlist.Add(t1235);
            teacherclass t1236 = new teacherclass(); t1236.name = "Ulrica Momqvist"; t1236.firstname = "Ulrica"; t1236.lastname = "Momqvist"; t1236.teacherID = "uli"; t1236.birthday = "740106"; teacherlist.Add(t1236);
            teacherclass t1237 = new teacherclass(); t1237.name = "Ulf Magnusson"; t1237.firstname = "Ulf"; t1237.lastname = "Magnusson"; t1237.teacherID = "uma"; t1237.birthday = "530829"; teacherlist.Add(t1237);
            teacherclass t1238 = new teacherclass(); t1238.name = "Ulf Modin"; t1238.firstname = "Ulf"; t1238.lastname = "Modin"; t1238.teacherID = "umd"; t1238.birthday = "590704"; teacherlist.Add(t1238);
            teacherclass t1239 = new teacherclass(); t1239.name = "Ulrika Norling"; t1239.firstname = "Ulrika"; t1239.lastname = "Norling"; t1239.teacherID = "unl"; t1239.birthday = "750226"; teacherlist.Add(t1239);
            teacherclass t1240 = new teacherclass(); t1240.name = "Ulf Nytell"; t1240.firstname = "Ulf"; t1240.lastname = "Nytell"; t1240.teacherID = "uny"; t1240.birthday = "551218"; teacherlist.Add(t1240);
            teacherclass t1241 = new teacherclass(); t1241.name = "Ulf Gardtman"; t1241.firstname = "Ulf"; t1241.lastname = "Gardtman"; t1241.teacherID = "uog"; t1241.birthday = "560910"; teacherlist.Add(t1241);
            teacherclass t1242 = new teacherclass(); t1242.name = "Ulrika Rehnström"; t1242.firstname = "Ulrika"; t1242.lastname = "Rehnström"; t1242.teacherID = "ure"; t1242.birthday = "730107"; teacherlist.Add(t1242);
            teacherclass t1243 = new teacherclass(); t1243.name = "Ulf Rydén"; t1243.firstname = "Ulf"; t1243.lastname = "Rydén"; t1243.teacherID = "ury"; t1243.birthday = "590224"; teacherlist.Add(t1243);
            teacherclass t1244 = new teacherclass(); t1244.name = "Ulf Sjöberg"; t1244.firstname = "Ulf"; t1244.lastname = "Sjöberg"; t1244.teacherID = "usj"; t1244.birthday = "390718"; teacherlist.Add(t1244);
            teacherclass t1245 = new teacherclass(); t1245.name = "Ulrika Sivertsson Nelzén"; t1245.firstname = "Ulrika"; t1245.lastname = "Sivertsson Nelzén"; t1245.teacherID = "usn"; t1245.birthday = "760712"; teacherlist.Add(t1245);
            teacherclass t1246 = new teacherclass(); t1246.name = "Ulrika Tornberg"; t1246.firstname = "Ulrika"; t1246.lastname = "Tornberg"; t1246.teacherID = "utg"; t1246.birthday = "400717"; teacherlist.Add(t1246);
            teacherclass t1247 = new teacherclass(); t1247.name = "Anna Westblom"; t1247.firstname = "Anna"; t1247.lastname = "Westblom"; t1247.teacherID = "wan"; t1247.birthday = "720812"; teacherlist.Add(t1247);
            teacherclass t1248 = new teacherclass(); t1248.name = "Wirginia Bogatic"; t1248.firstname = "Wirginia"; t1248.lastname = "Bogatic"; t1248.teacherID = "wbo"; t1248.birthday = "690309"; teacherlist.Add(t1248);
            teacherclass t1249 = new teacherclass(); t1249.name = "Voicu Brabie"; t1249.firstname = "Voicu"; t1249.lastname = "Brabie"; t1249.teacherID = "vbr"; t1249.birthday = "421022"; teacherlist.Add(t1249);
            teacherclass t1250 = new teacherclass(); t1250.name = "Veronica De Majo"; t1250.firstname = "Veronica De"; t1250.lastname = "Majo"; t1250.teacherID = "vdm"; t1250.birthday = "740716"; teacherlist.Add(t1250);
            teacherclass t1251 = new teacherclass(); t1251.name = "Vilmantas Giedraitis"; t1251.firstname = "Vilmantas"; t1251.lastname = "Giedraitis"; t1251.teacherID = "vgi"; t1251.birthday = "710529"; teacherlist.Add(t1251);
            teacherclass t1252 = new teacherclass(); t1252.name = "Wei Hing Rosenkvist"; t1252.firstname = "Wei Hing"; t1252.lastname = "Rosenkvist"; t1252.teacherID = "whi"; t1252.birthday = "721021"; teacherlist.Add(t1252);
            teacherclass t1253 = new teacherclass(); t1253.name = "Hyseni Vesel"; t1253.firstname = "Hyseni"; t1253.lastname = "Vesel"; t1253.teacherID = "vhy"; t1253.birthday = "890802"; teacherlist.Add(t1253);
            teacherclass t1254 = new teacherclass(); t1254.name = "Viktor Johansson"; t1254.firstname = "Viktor"; t1254.lastname = "Johansson"; t1254.teacherID = "vjo"; t1254.birthday = "790904"; teacherlist.Add(t1254);
            teacherclass t1255 = new teacherclass(); t1255.name = "Victoria Kihlström"; t1255.firstname = "Victoria"; t1255.lastname = "Kihlström"; t1255.teacherID = "vki"; t1255.birthday = "861211"; teacherlist.Add(t1255);
            teacherclass t1256 = new teacherclass(); t1256.name = "Veronica Matsson"; t1256.firstname = "Veronica"; t1256.lastname = "Matsson"; t1256.teacherID = "vma"; t1256.birthday = "760730"; teacherlist.Add(t1256);
            teacherclass t1257 = new teacherclass(); t1257.name = "Viktor Nordgren"; t1257.firstname = "Viktor"; t1257.lastname = "Nordgren"; t1257.teacherID = "vno"; t1257.birthday = "880713"; teacherlist.Add(t1257);
            teacherclass t1258 = new teacherclass(); t1258.name = "Vera Nigrisoli Wärnhjelm"; t1258.firstname = "Vera"; t1258.lastname = "Nigrisoli Wärnhjelm"; t1258.teacherID = "vnw"; t1258.birthday = "590627"; teacherlist.Add(t1258);
            teacherclass t1259 = new teacherclass(); t1259.name = "Vijay Pratap Paidi"; t1259.firstname = "Vijay"; t1259.lastname = "Pratap Paidi"; t1259.teacherID = "vpp"; t1259.birthday = "860426"; teacherlist.Add(t1259);
            teacherclass t1260 = new teacherclass(); t1260.name = "Victoria Rosenström"; t1260.firstname = "Victoria"; t1260.lastname = "Rosenström"; t1260.teacherID = "vro"; t1260.birthday = "871205"; teacherlist.Add(t1260);
            teacherclass t1261 = new teacherclass(); t1261.name = "Vanja Smalc"; t1261.firstname = "Vanja"; t1261.lastname = "Smalc"; t1261.teacherID = "vsm"; t1261.birthday = "851003"; teacherlist.Add(t1261);
            teacherclass t1262 = new teacherclass(); t1262.name = "Wei Song"; t1262.firstname = "Wei"; t1262.lastname = "Song"; t1262.teacherID = "wso"; t1262.birthday = "600407"; teacherlist.Add(t1262);
            teacherclass t1263 = new teacherclass(); t1263.name = "Stefan Weinholz"; t1263.firstname = "Stefan"; t1263.lastname = "Weinholz"; t1263.teacherID = "wst"; t1263.birthday = "620118"; teacherlist.Add(t1263);
            teacherclass t1264 = new teacherclass(); t1264.name = "Viktoria Törnqvist"; t1264.firstname = "Viktoria"; t1264.lastname = "Törnqvist"; t1264.teacherID = "vto"; t1264.birthday = "740113"; teacherlist.Add(t1264);
            teacherclass t1265 = new teacherclass(); t1265.name = "Viktoria Waagaard"; t1265.firstname = "Viktoria"; t1265.lastname = "Waagaard"; t1265.teacherID = "vwa"; t1265.birthday = "670206"; teacherlist.Add(t1265);
            teacherclass t1266 = new teacherclass(); t1266.name = "Viola Petren"; t1266.firstname = "Viola"; t1266.lastname = "Petren"; t1266.teacherID = "vyl"; t1266.birthday = "420515"; teacherlist.Add(t1266);
            teacherclass t1267 = new teacherclass(); t1267.name = "Xuan Chen"; t1267.firstname = "Xuan"; t1267.lastname = "Chen"; t1267.teacherID = "xch"; t1267.birthday = "830527"; teacherlist.Add(t1267);
            teacherclass t1268 = new teacherclass(); t1268.name = "Xiangli Meng"; t1268.firstname = "Xiangli"; t1268.lastname = "Meng"; t1268.teacherID = "xme"; t1268.birthday = "850720"; teacherlist.Add(t1268);
            teacherclass t1269 = new teacherclass(); t1269.name = "Xiaoyun Zhao"; t1269.firstname = "Xiaoyun"; t1269.lastname = "Zhao"; t1269.teacherID = "xzh"; t1269.birthday = "860610"; teacherlist.Add(t1269);
            teacherclass t1270 = new teacherclass(); t1270.name = "Yagiz Azizoglu"; t1270.firstname = "Yagiz"; t1270.lastname = "Azizoglu"; t1270.teacherID = "yaz"; t1270.birthday = "860709"; teacherlist.Add(t1270);
            teacherclass t1271 = new teacherclass(); t1271.name = "Yngve Bergström"; t1271.firstname = "Yngve"; t1271.lastname = "Bergström"; t1271.teacherID = "ybe"; t1271.birthday = "401229"; teacherlist.Add(t1271);
            teacherclass t1272 = new teacherclass(); t1272.name = "Yvonne Blomberg"; t1272.firstname = "Yvonne"; t1272.lastname = "Blomberg"; t1272.teacherID = "ybl"; t1272.birthday = "550501"; teacherlist.Add(t1272);
            teacherclass t1273 = new teacherclass(); t1273.name = "Yngve Bergqvist"; t1273.firstname = "Yngve"; t1273.lastname = "Bergqvist"; t1273.teacherID = "ybq"; t1273.birthday = "440619"; teacherlist.Add(t1273);
            teacherclass t1274 = new teacherclass(); t1274.name = "Ylva Cedervall"; t1274.firstname = "Ylva"; t1274.lastname = "Cedervall"; t1274.teacherID = "yce"; t1274.birthday = "600225"; teacherlist.Add(t1274);
            teacherclass t1275 = new teacherclass(); t1275.name = "Yanina Espegren"; t1275.firstname = "Yanina"; t1275.lastname = "Espegren"; t1275.teacherID = "yes"; t1275.birthday = "800127"; teacherlist.Add(t1275);
            teacherclass t1276 = new teacherclass(); t1276.name = "Yangfan Hultgren"; t1276.firstname = "Yangfan"; t1276.lastname = "Hultgren"; t1276.teacherID = "yhu"; t1276.birthday = "690124"; teacherlist.Add(t1276);
            teacherclass t1277 = new teacherclass(); t1277.name = "Yvonne Karlsson"; t1277.firstname = "Yvonne"; t1277.lastname = "Karlsson"; t1277.teacherID = "yka"; t1277.birthday = "580521"; teacherlist.Add(t1277);
            teacherclass t1278 = new teacherclass(); t1278.name = "Yoko Kumagai"; t1278.firstname = "Yoko"; t1278.lastname = "Kumagai"; t1278.teacherID = "yku"; t1278.birthday = "760802"; teacherlist.Add(t1278);
            teacherclass t1279 = new teacherclass(); t1279.name = "Yujiao Li"; t1279.firstname = "Yujiao"; t1279.lastname = "Li"; t1279.teacherID = "yli"; t1279.birthday = "861026"; teacherlist.Add(t1279);
            teacherclass t1280 = new teacherclass(); t1280.name = "Antti Ylikiiskilä"; t1280.firstname = "Antti"; t1280.lastname = "Ylikiiskilä"; t1280.teacherID = "ylik"; t1280.birthday = "521125"; teacherlist.Add(t1280);
            teacherclass t1281 = new teacherclass(); t1281.name = "Yoko Mizufune"; t1281.firstname = "Yoko"; t1281.lastname = "Mizufune"; t1281.teacherID = "ymi"; t1281.birthday = "790824"; teacherlist.Add(t1281);
            teacherclass t1282 = new teacherclass(); t1282.name = "Ylwa Rohlén"; t1282.firstname = "Ylwa"; t1282.lastname = "Rohlén"; t1282.teacherID = "yro"; t1282.birthday = "520217"; teacherlist.Add(t1282);
            teacherclass t1283 = new teacherclass(); t1283.name = "Ylva Sundmark"; t1283.firstname = "Ylva"; t1283.lastname = "Sundmark"; t1283.teacherID = "ysu"; t1283.birthday = "631120"; teacherlist.Add(t1283);
            teacherclass t1284 = new teacherclass(); t1284.name = "Yu Wang"; t1284.firstname = "Yu"; t1284.lastname = "Wang"; t1284.teacherID = "ywa"; t1284.birthday = "760804"; teacherlist.Add(t1284);
            teacherclass t1285 = new teacherclass(); t1285.name = "Ylva Granbom"; t1285.firstname = "Ylva"; t1285.lastname = "Granbom"; t1285.teacherID = "yvg"; t1285.birthday = "710705"; teacherlist.Add(t1285);
            teacherclass t1286 = new teacherclass(); t1286.name = "Yuan-Yuan Wu"; t1286.firstname = "Yuan-Yuan"; t1286.lastname = "Wu"; t1286.teacherID = "yyw"; t1286.birthday = "821126"; teacherlist.Add(t1286);
            teacherclass t1287 = new teacherclass(); t1287.name = "Zelal Bal"; t1287.firstname = "Zelal"; t1287.lastname = "Bal"; t1287.teacherID = "zba"; t1287.birthday = "780724"; teacherlist.Add(t1287);
            teacherclass t1288 = new teacherclass(); t1288.name = "Zandra Granath"; t1288.firstname = "Zandra"; t1288.lastname = "Granath"; t1288.teacherID = "zkg"; t1288.birthday = "740502"; teacherlist.Add(t1288);
            teacherclass t1289 = new teacherclass(); t1289.name = "Zuzana Macuchova"; t1289.firstname = "Zuzana"; t1289.lastname = "Macuchova"; t1289.teacherID = "zma"; t1289.birthday = "790802"; teacherlist.Add(t1289);
            extra_sigdict.Add("amir satari","ami");
            extra_sigdict.Add("arantxa santos muñoz","asz");
            extra_sigdict.Add(@"c:\dotnwb3\tgs\def ht-16\hm\svenska\tgs def sv 16_17.xlsx:anna å","anj");
            extra_sigdict.Add("camilla udo","cud");
            extra_sigdict.Add("camilla videlöf","cwr");
            extra_sigdict.Add("carles magriña","cmb");
            extra_sigdict.Add("cecilla strandroth","csr");
            extra_sigdict.Add("chris bales","cba");
            extra_sigdict.Add("christina von post","cvp");
            extra_sigdict.Add("daniel walentin","dwn");
            extra_sigdict.Add("dávid molnár","mdi");
            extra_sigdict.Add("desiree kroner","dkr");
            extra_sigdict.Add("elisabet nerpin","ene");
            extra_sigdict.Add("fadi abu deeb","fad");
            extra_sigdict.Add("fredri hartwig","fhr");
            extra_sigdict.Add("göran moren","gmo");
            extra_sigdict.Add("helén sterner","hse");
            extra_sigdict.Add("henrik karlsson","hka");
            extra_sigdict.Add("ingela spegel- nääs","isp");
            extra_sigdict.Add("jan  åkerstedt","jak");
            extra_sigdict.Add("juvas marianne liljas","jml");
            extra_sigdict.Add("kamal abu deeb","kad");
            extra_sigdict.Add("maria delden","mde");
            extra_sigdict.Add("maria melin","mku");
            extra_sigdict.Add("maria sjöberg fredriksson","mfd");
            extra_sigdict.Add("marit nybelius","mny");
            extra_sigdict.Add("nikita mikhailov","nmi");
            extra_sigdict.Add("nima safara nosar","nsn");
            extra_sigdict.Add("ninni johnsson wallin","njw");
            extra_sigdict.Add("ninni vallfelt","nwa");
            extra_sigdict.Add("pelle eriksson","pek");
            extra_sigdict.Add("rina navarro","rna");
            extra_sigdict.Add("roger hjortendal","rhj");
            extra_sigdict.Add("sven olov (totte) mattsson","som");
            extra_sigdict.Add("therese hercules","thr");
            extra_sigdict.Add("tobias feldreich","trf");
            extra_sigdict.Add("aldo lentina", "alt");
            extra_sigdict.Add("andréev kostia", "kan");
            extra_sigdict.Add("arantxa santos", "asz");
            extra_sigdict.Add("aranxta santos nunoz", "asz");
            extra_sigdict.Add("astrid alnås widen", "aaw");
            extra_sigdict.Add("berit karlsson wallin", "bkr");
            extra_sigdict.Add("carolina león vegas", "clv");
            extra_sigdict.Add("catharina nyström höög", "cnh");
            extra_sigdict.Add("cox eriksson", "cce");
            extra_sigdict.Add("klingberg fridner sara", "sfk");
            extra_sigdict.Add("larsson joacim", "jlr");
            extra_sigdict.Add("lena marmståhl hammar", "lma");
            extra_sigdict.Add("lisa karlstrom-rosell", "lkr");
            extra_sigdict.Add("marco hernandes velasquez", "mhv");
            extra_sigdict.Add("marie klingberg-allvin", "mkl");
            extra_sigdict.Add("mario lópez cordero", "mlc");
            extra_sigdict.Add("mariya niendorf", "mln");
            extra_sigdict.Add("masako thor", "mtr");
            extra_sigdict.Add("monique toratti-lindgren", "moi");
            extra_sigdict.Add("moström åberg marie", "mmt");
            extra_sigdict.Add("nicole nerström", "nne");
            extra_sigdict.Add("ninni jonsson wallin", "njw");
            extra_sigdict.Add("olsson bertil", "beo");
            extra_sigdict.Add("pascal reybreyend", "prb");
            extra_sigdict.Add("renee flacking", "rfl");
            extra_sigdict.Add("susanna heldt-cassel", "shc");
            extra_sigdict.Add("theres rodin", "trd");
            extra_sigdict.Add("totti allard", "taa");
            extra_sigdict.Add("zamorano llena carmen", "cza");
            extra_sigdict.Add("åkerstedt anna", "anj");
            extra_sigdict.Add("ädel anneli", "aal");
            extra_sigdict.Add("marie", "mmt");
            extra_sigdict.Add("alexis", "ary");
            extra_sigdict.Add("ing-marie", "ima");
            extra_sigdict.Add("karl w", "kws");
            extra_sigdict.Add("karl g", "kgu");
            extra_sigdict.Add("carl b", "cob");
            extra_sigdict.Add("bengt p", "bpo");
            extra_sigdict.Add("åsa", "asv");
            extra_sigdict.Add("alexander", "alk");
            extra_sigdict.Add("michael", "mii");
            extra_sigdict.Add("fiffi", "jce");
            extra_sigdict.Add("petter", "pkt");
            extra_sigdict.Add("gustav", "gbk");
            extra_sigdict.Add("brandt", "dbr");
            extra_sigdict.Add("heldt-cassel", "shc");
            extra_sigdict.Add("pashkevich", "alp");
            extra_sigdict.Add("farsari", "ifa");
            extra_sigdict.Add("thulemark", "mth");
            extra_sigdict.Add("macuchova", "zma");
            extra_sigdict.Add("engström", "ceg");
            extra_sigdict.Add("yachin", "jmy");
            extra_sigdict.Add("landström", "mat");
            extra_sigdict.Add("lindgren", "clg");
            extra_sigdict.Add("wikström", "dwi");

            foreach (string name in extra_sigdict.Keys)
            {
                var tq = (from t in teacherlist where t.teacherID == extra_sigdict[name] select t);
                if (tq.Count() > 0)
                    continue;
                teacherclass tc = new teacherclass();
                tc.name = name;
                string[] names = name.Split();
                tc.firstname = names[0];
                tc.lastname = names[names.Length - 1];
                tc.teacherID = extra_sigdict[name];
                teacherlist.Add(tc);
            }

            subjectnamedict.Add("byggteknik","byggteknik");
            subjectnamedict.Add("byggtek/","byggteknik");
            subjectnamedict.Add("kemiteknik","kemiteknik");
            subjectnamedict.Add("energi och miljöteknik","energiteknik");
            subjectnamedict.Add("energiteknik","energiteknik");
            subjectnamedict.Add("energi och miljö","energiteknik");
            //subjectnamedict.Add("energiteknik","energiteknik");
            subjectnamedict.Add("enrtrgi & miljöteknik","energiteknik");
            //subjectnamedict.Add("energi och miljö","energiteknik");
            subjectnamedict.Add("energi & miljöteknik","energiteknik");
            subjectnamedict.Add("företagsekonomi","företagsekonomi");
            subjectnamedict.Add("fek","företagsekonomi");
            subjectnamedict.Add("industri och samhälle","industri och samhälle");
            //subjectnamedict.Add("fek","företagsekonomi");
            subjectnamedict.Add("matematik","matematik");
            subjectnamedict.Add("ie","industriell ekonomi");
            subjectnamedict.Add("indek","industriell ekonomi");
            subjectnamedict.Add("maskinteknik","maskinteknik");
            subjectnamedict.Add("maskin","maskinteknik");
            subjectnamedict.Add("materialteknik","materialteknik");
            subjectnamedict.Add("materialvetenskap","materialteknik");
            subjectnamedict.Add("materialvetenakap","materialteknik");
            subjectnamedict.Add("matertialteknik","materialteknik");
            subjectnamedict.Add("rva","rva");
            subjectnamedict.Add("skog och trä","skogsteknik");
            subjectnamedict.Add("skog och träteknik","skogsteknik");

            subjectnamedict.Add("historia (religion)","historia");
            subjectnamedict.Add("arabiska","arabiska");

            subjectnamedict.Add("energi och miljlöteknik","energiteknik");
            subjectnamedict.Add("bp","bildproduktion");
            subjectnamedict.Add("bp/li","bildproduktion");
            subjectnamedict.Add("bild","bild");
            subjectnamedict.Add("engelska","engelska");
            subjectnamedict.Add("filosofi","filosofi");
            subjectnamedict.Add("historia","historia");

            subjectnamedict.Add("italienska","italienska");
            subjectnamedict.Add("italiensa","italienska");
            subjectnamedict.Add("japanska","japanska");
            subjectnamedict.Add("kinesiska ","kinesiska ");
            subjectnamedict.Add("kinesiska","kinesiska");
            subjectnamedict.Add("litteraturvetenskap","litteraturvetenskap");
            subjectnamedict.Add("litvet","litteraturvetenskap");
            subjectnamedict.Add("littvet","litteraturvetenskap");
            subjectnamedict.Add("portugisiska ","portugisiska ");
            subjectnamedict.Add("ljudproduktion","ljud- och musikproduktion");
            subjectnamedict.Add("religionsvetenskap","religionsvetenskap");

            subjectnamedict.Add("religion","religionsvetenskap");
            subjectnamedict.Add("spanska","spanska");
            subjectnamedict.Add("sva","svenska som andraspråk");
            subjectnamedict.Add("svenska som andraspråk","svenska som andraspråk");
            subjectnamedict.Add("svenska som anraspråk","svenska som andraspråk");
            subjectnamedict.Add("svenska språket","svenska");

            subjectnamedict.Add("svenska","svenska");
            subjectnamedict.Add("sv","svenska");
            subjectnamedict.Add("tyska","tyska");


            subjectnamedict.Add("ihv","idrotts- och hälsovetenskap");
            subjectnamedict.Add("geografi","geografi");
            subjectnamedict.Add("idrotts- och hälsovetenskap","idrotts- och hälsovetenskap");
            subjectnamedict.Add("idrott- och hälsovetenskap","idrotts- och hälsovetenskap");
            subjectnamedict.Add("medicinsk vetenskap","medicinsk vetenskap");
            subjectnamedict.Add("idrotts och hälsovet.","idrotts- och hälsovetenskap");
            subjectnamedict.Add("ma.did.","matematikdidaktik");
            subjectnamedict.Add("matematik didaktik","matematikdidaktik");
            subjectnamedict.Add("matematikdidaktik","matematikdidaktik");
            subjectnamedict.Add("madid","matematikdidaktik");
            subjectnamedict.Add("ma did","matematikdidaktik");
            //subjectnamedict.Add("matematik didaktik","matematikdidaktik");
            //subjectnamedict.Add("matematikdidaktik","matematikdidaktik");
            subjectnamedict.Add("omvårdnad","omvårdnad");
            subjectnamedict.Add("medicinskvetenskap","medicinsk vetenskap");
            subjectnamedict.Add("medicin","medicinsk vetenskap");

            //subjectnamedict.Add("medicinsk vetenskap","medicinsk vetenskap");
            subjectnamedict.Add("medvet","medicinsk vetenskap");
            subjectnamedict.Add("med vet","medicinsk vetenskap");
            subjectnamedict.Add("naturvetenskap","naturvetenskap");

            subjectnamedict.Add("omv","omvårdnad");
            //subjectnamedict.Add("omvårdnad","omvårdnad");
            subjectnamedict.Add("omvårdand","omvårdnad");
            subjectnamedict.Add("ped arb","pedagogiskt arbete");
            subjectnamedict.Add("socant","pedagogiskt arbete");
            subjectnamedict.Add("ped.arb.","pedagogiskt arbete");
            subjectnamedict.Add("pedagogiskt arbete","pedagogiskt arbete");
            //subjectnamedict.Add("ped arb","pedagogiskt arbete");
            subjectnamedict.Add("pedagogiskr arbete","pedagogiskt arbete");
            //subjectnamedict.Add("pedagogiskt arbete","pedagogiskt arbete");
            subjectnamedict.Add("pedagogik","pedagogik");
            subjectnamedict.Add("pedagogik /pud","pedagogik");
            subjectnamedict.Add("sud/ socialt arbete","socialt arbete");
            subjectnamedict.Add("socialt arbete","socialt arbete");

            subjectnamedict.Add("sociologi","sociologi");

            subjectnamedict.Add("statsvetenskap","statsvetenskap");

            subjectfolderdict.Add(@"\Afr Studies\","african studies");
            subjectfolderdict.Add(@"\Arabiska\","arabiska");
            subjectfolderdict.Add(@"\Bild\","bild");
            subjectfolderdict.Add(@"\BP\","bildproduktion");
            subjectfolderdict.Add(@"\Engelska\","engelska");
            subjectfolderdict.Add(@"\Franska\","franska");
            subjectfolderdict.Add(@"\Filosofi\","filosofi");
            subjectfolderdict.Add(@"\Historia\","historia");
            subjectfolderdict.Add(@"\Ital\","italienska");
            subjectfolderdict.Add(@"\Japanska\","japanska");
            subjectfolderdict.Add(@"\Kin\","kinesiska");
            subjectfolderdict.Add(@"\Littvet\","litteraturvetenskap");
            subjectfolderdict.Add(@"\LMP\","ljud- och musikproduktion");
            subjectfolderdict.Add(@"\Portugisiska\","portugisiska");
            subjectfolderdict.Add(@"\Religion\","religionsvetenskap");
            subjectfolderdict.Add(@"\Ryska\","ryska");
            subjectfolderdict.Add(@"\Spanska\","spanska");
            subjectfolderdict.Add(@"\SVA\","svenska som andraspråk");
            subjectfolderdict.Add(@"\Svenska\","svenska");
            subjectfolderdict.Add(@"\Tyska\","tyska");

            subjectfolderdict.Add(@"\IHV\","idrotts- och hälsovetenskap");
            subjectfolderdict.Add(@"\Mattedidaktiken\","matematikdidaktik");
            subjectfolderdict.Add(@"\Med vet\","medicinsk vetenskap");
            subjectfolderdict.Add(@"\Naturvet\","naturvetenskap");
            subjectfolderdict.Add(@"\OMV\","omvårdnad");
            subjectfolderdict.Add(@"\Ped arb\","pedagogiskt arbete");
            subjectfolderdict.Add(@"\Soc Arb\","socialt arbete");
            subjectfolderdict.Add(@"\Sociologi\","sociologi");
            subjectfolderdict.Add(@"\Statsvet\","statsvetenskap");

            subjectfolderdict.Add(@"\Bygg\","byggteknik");
            subjectfolderdict.Add(@"\Dta old\","datateknik");
            subjectfolderdict.Add(@"\Energiteknik\","energiteknik");
            subjectfolderdict.Add(@"\Entreprenörskap\","entreprenörskap");
            subjectfolderdict.Add(@"\eta\","eta");
            subjectfolderdict.Add(@"\FEK\","företagsekonomi");
            subjectfolderdict.Add(@"\Fysik\","fysik");
            subjectfolderdict.Add(@"\GT\","grafisk teknologi");
            subjectfolderdict.Add(@"\INDEK\","industriell ekonomi");
            subjectfolderdict.Add(@"\informatik\","informatik");
            subjectfolderdict.Add(@"\Kemiteknik\","kemiteknik");
            subjectfolderdict.Add(@"\maskinteknik\","maskinteknik");
            subjectfolderdict.Add(@"\Matematik\","matematik");
            subjectfolderdict.Add(@"\materialvetenskap\","materialteknik");
            subjectfolderdict.Add(@"\Rättsvetenskap\","rättsvetenskap");
            subjectfolderdict.Add(@"\Skogsteknik\","skogsteknik");

            subjectcodedict.Add("MT", "maskinteknik");
            subjectcodedict.Add("GT", "grafisk teknologi");
            subjectcodedict.Add("MD", "matematikdidaktik");
            subjectcodedict.Add("EU", "entreprenörskap");
            subjectcodedict.Add("HI", "historia");
            subjectcodedict.Add("VÅ", "omvårdnad");
            subjectcodedict.Add("RK", "religionsvetenskap");
            subjectcodedict.Add("MC", "medicinsk vetenskap");
            subjectcodedict.Add("EN", "engelska");
            subjectcodedict.Add("LP", "ljud- och musikproduktion");
            subjectcodedict.Add("KG", "kulturgeografi");
            subjectcodedict.Add("MP", "materialteknik");
            subjectcodedict.Add("IH", "idrotts- och hälsovetenskap");
            subjectcodedict.Add("IK", "informatik");
            subjectcodedict.Add("SW", "skogsteknik");
            subjectcodedict.Add("AR", "arabiska");
            subjectcodedict.Add("PA", "personal och arbetsliv");
            subjectcodedict.Add("AB", "arbetsvetenskap");
            subjectcodedict.Add("BY", "byggteknik");
            subjectcodedict.Add("BQ", "bildproduktion");
            subjectcodedict.Add("RV", "rättsvetenskap");
            subjectcodedict.Add("DT", "datateknik");
            subjectcodedict.Add("PE", "pedagogik");
            subjectcodedict.Add("PG", "pedagogiskt arbete");
            subjectcodedict.Add("SA", "socialt arbete");
            subjectcodedict.Add("FÖ", "företagsekonomi");
            subjectcodedict.Add("SS", "svenska som andraspråk");
            subjectcodedict.Add("AU", "audiovisuella studier");
            subjectcodedict.Add("LI", "litteraturvetenskap");
            subjectcodedict.Add("SH", "samhällskunskap");
            subjectcodedict.Add("ST", "statistik");
            subjectcodedict.Add("BP", "bild");
            subjectcodedict.Add("MÖ", "miljöteknik");
            subjectcodedict.Add("BI", "biologi");
            subjectcodedict.Add("IE", "industriell ekonomi");
            subjectcodedict.Add("SP", "spanska");
            subjectcodedict.Add("PR", "portugisiska");
            subjectcodedict.Add("RY", "ryska");
            subjectcodedict.Add("ET", "elektroteknik");
            subjectcodedict.Add("NA", "nationalekonomi");
            subjectcodedict.Add("SK", "statsvetenskap");
            subjectcodedict.Add("GG", "geografi");
            subjectcodedict.Add("TR", "turismvetenskap");
            subjectcodedict.Add("VV", "vårdvetenskap");
            subjectcodedict.Add("MI", "mikrodataanalys");
            subjectcodedict.Add("MK", "medie- och kommunikationsvetenskap");
            subjectcodedict.Add("FR", "franska");
            subjectcodedict.Add("FY", "fysik");
            subjectcodedict.Add("SO", "sociologi");
            subjectcodedict.Add("NV", "naturvetenskap");
            subjectcodedict.Add("PS", "psykologi");
            subjectcodedict.Add("IT", "italienska");
            subjectcodedict.Add("JP", "japanska");
            subjectcodedict.Add("KE", "kemi");
            subjectcodedict.Add("KT", "kemiteknik");
            subjectcodedict.Add("KI", "kinesiska");
            subjectcodedict.Add("KL", "kulturvetenskap");
            subjectcodedict.Add("MA", "matematik");
            subjectcodedict.Add("FI", "filosofi");
            subjectcodedict.Add("SV", "svenska");
            subjectcodedict.Add("TY", "tyska");
            subjectcodedict.Add("AS", "african studies");
            subjectcodedict.Add("EG", "energiteknik");
            subjectcodedict.Add("SQ", "samhällsbyggnadsteknik");
            subjectcodedict.Add("SR", "sexuell reproduktiv perinatal hälsa");
            subjectcodedict.Add("BE", "bergteknik");
            subjectcodedict.Add("SB", "socialantropologi");
            subjectcodedict.Add("VU", "oral hälsa");


            courseorgdict.Add("maskinteknik", "maskinteknik");
            courseorgdict.Add("grafisk teknologi", "grafisk teknologi");
            courseorgdict.Add("matematikdidaktik", "matematikdidaktik");
            courseorgdict.Add("entreprenörskap", "entreprenörskap");
            courseorgdict.Add("historia", "historia");
            courseorgdict.Add("omvårdnad", "omvårdnad");
            courseorgdict.Add("religionsvetenskap", "religionsvetenskap");
            courseorgdict.Add("medicinsk vetenskap", "medicinsk vetenskap");
            courseorgdict.Add("engelska", "engelska");
            courseorgdict.Add("ljud- och musikproduktion", "ljud- och musikproduktion");
            courseorgdict.Add("kulturgeografi", "kulturgeografi");
            courseorgdict.Add("materialteknik", "materialteknik");
            courseorgdict.Add("idrotts- och hälsovetenskap", "idrotts- och hälsovetenskap");
            courseorgdict.Add("informatik", "informatik");
            courseorgdict.Add("skogsteknik", "skogsteknik");
            courseorgdict.Add("arabiska", "arabiska");
            courseorgdict.Add("personal och arbetsliv", "PAL");
            courseorgdict.Add("arbetsvetenskap", "arbetsvetenskap");
            courseorgdict.Add("byggteknik", "byggteknik");
            courseorgdict.Add("bildproduktion", "bildproduktion");
            courseorgdict.Add("rättsvetenskap", "rättsvetenskap");
            courseorgdict.Add("datateknik", "datateknik");
            courseorgdict.Add("pedagogik", "pedagogik");
            courseorgdict.Add("pedagogiskt arbete", "pedagogiskt arbete");
            courseorgdict.Add("socialt arbete", "socialt arbete");
            courseorgdict.Add("företagsekonomi", "företagsekonomi");
            courseorgdict.Add("svenska som andraspråk", "svenska som andraspråk");
            courseorgdict.Add("audiovisuella studier", "ljud- och musikproduktion");
            courseorgdict.Add("litteraturvetenskap", "litteraturvetenskap");
            courseorgdict.Add("samhällskunskap", "samhällskunskap");
            courseorgdict.Add("statistik", "statistik");
            courseorgdict.Add("bild", "bild");
            courseorgdict.Add("miljöteknik", "energiteknik");
            courseorgdict.Add("biologi", "naturvetenskap");
            courseorgdict.Add("industriell ekonomi", "industriell ekonomi");
            courseorgdict.Add("spanska", "spanska");
            courseorgdict.Add("portugisiska", "portugisiska");
            courseorgdict.Add("ryska", "ryska");
            courseorgdict.Add("elektroteknik", "energiteknik");
            courseorgdict.Add("nationalekonomi", "nationalekonomi");
            courseorgdict.Add("statsvetenskap", "statsvetenskap");
            courseorgdict.Add("geografi", "geografi");
            courseorgdict.Add("turismvetenskap", "turism");
            courseorgdict.Add("vårdvetenskap", "omvårdnad");
            courseorgdict.Add("mikrodataanalys", "mikrodata");
            courseorgdict.Add("medie- och kommunikationsvetenskap", "ljud- och musikproduktion");
            courseorgdict.Add("sexuell reproduktiv perinatal hälsa", "srph");
            courseorgdict.Add("franska", "franska");
            courseorgdict.Add("fysik", "fysik");
            courseorgdict.Add("sociologi", "sociologi");
            courseorgdict.Add("naturvetenskap", "naturvetenskap");
            courseorgdict.Add("psykologi", "pedagogik");
            courseorgdict.Add("italienska", "italienska");
            courseorgdict.Add("japanska", "japanska");
            courseorgdict.Add("kemi", "naturvetenskap");
            courseorgdict.Add("kemiteknik", "kemiteknik");
            courseorgdict.Add("kinesiska", "kinesiska");
            courseorgdict.Add("kulturvetenskap", "ämne saknas");
            courseorgdict.Add("matematik", "matematik");
            courseorgdict.Add("filosofi", "filosofi");
            courseorgdict.Add("svenska", "svenska");
            courseorgdict.Add("tyska", "tyska");
            courseorgdict.Add("african studies", "african studies");
            courseorgdict.Add("energiteknik", "energiteknik");
            courseorgdict.Add("samhällsbyggnadsteknik", "samhällsbyggnadsteknik");
            courseorgdict.Add("bergteknik", "byggteknik");
            courseorgdict.Add("socialantropologi", "sociologi");
            courseorgdict.Add("oral hälsa", "omvårdnad");

            orgsubjectclass os0 = new orgsubjectclass(); os0.osname = "ämne saknas"; os0.academy = "???"; os0.department = "avdelning saknas"; os0.objekt = 0; orgsubjectlist.Add(os0);

            orgsubjectclass os1 = new orgsubjectclass(); os1.osname = "bild"; os1.academy = "HM"; os1.department = "medier"; os1.objekt = 43421000; orgsubjectlist.Add(os1);
            orgsubjectclass os2 = new orgsubjectclass(); os2.osname = "bildproduktion"; os2.academy = "HM"; os2.department = "medier"; os2.objekt = 43431000; orgsubjectlist.Add(os2);
            orgsubjectclass os3 = new orgsubjectclass(); os3.osname = "ljud- och musikproduktion"; os3.academy = "HM"; os3.department = "medier"; os3.objekt = 43461000; orgsubjectlist.Add(os3);
            orgsubjectclass os4 = new orgsubjectclass(); os4.osname = "arabiska"; os4.academy = "HM"; os4.department = "språk"; os4.objekt = 43551000; orgsubjectlist.Add(os4);
            orgsubjectclass os5 = new orgsubjectclass(); os5.osname = "japanska"; os5.academy = "HM"; os5.department = "språk"; os5.objekt = 43561000; orgsubjectlist.Add(os5);
            orgsubjectclass os6 = new orgsubjectclass(); os6.osname = "kinesiska"; os6.academy = "HM"; os6.department = "språk"; os6.objekt = 43571000; orgsubjectlist.Add(os6);
            orgsubjectclass os7 = new orgsubjectclass(); os7.osname = "portugisiska"; os7.academy = "HM"; os7.department = "språk"; os7.objekt = 43581000; orgsubjectlist.Add(os7);
            orgsubjectclass os8 = new orgsubjectclass(); os8.osname = "ryska"; os8.academy = "HM"; os8.department = "språk"; os8.objekt = 43591000; orgsubjectlist.Add(os8);
            orgsubjectclass os9 = new orgsubjectclass(); os9.osname = "engelska"; os9.academy = "HM"; os9.department = "språk"; os9.objekt = 43821000; orgsubjectlist.Add(os9);
            orgsubjectclass os10 = new orgsubjectclass(); os10.osname = "franska"; os10.academy = "HM"; os10.department = "språk"; os10.objekt = 43831000; orgsubjectlist.Add(os10);
            orgsubjectclass os11 = new orgsubjectclass(); os11.osname = "svenska"; os11.academy = "HM"; os11.department = "humaniora"; os11.objekt = 43841000; orgsubjectlist.Add(os11);
            orgsubjectclass os12 = new orgsubjectclass(); os12.osname = "italienska"; os12.academy = "HM"; os12.department = "språk"; os12.objekt = 43851000; orgsubjectlist.Add(os12);
            orgsubjectclass os13 = new orgsubjectclass(); os13.osname = "litteraturvetenskap"; os13.academy = "HM"; os13.department = "humaniora"; os13.objekt = 43871000; orgsubjectlist.Add(os13);
            orgsubjectclass os14 = new orgsubjectclass(); os14.osname = "spanska"; os14.academy = "HM"; os14.department = "språk"; os14.objekt = 43881000; orgsubjectlist.Add(os14);
            orgsubjectclass os15 = new orgsubjectclass(); os15.osname = "historia"; os15.academy = "HM"; os15.department = "humaniora"; os15.objekt = 43891000; orgsubjectlist.Add(os15);
            orgsubjectclass os16 = new orgsubjectclass(); os16.osname = "tyska"; os16.academy = "HM"; os16.department = "språk"; os16.objekt = 43901000; orgsubjectlist.Add(os16);
            orgsubjectclass os17 = new orgsubjectclass(); os17.osname = "filosofi"; os17.academy = "HM"; os17.department = "humaniora"; os17.objekt = 43941000; orgsubjectlist.Add(os17);
            orgsubjectclass os18 = new orgsubjectclass(); os18.osname = "svenska som andraspråk"; os18.academy = "HM"; os18.department = "humaniora"; os18.objekt = 43951000; orgsubjectlist.Add(os18);
            orgsubjectclass os19 = new orgsubjectclass(); os19.osname = "african studies"; os19.academy = "HM"; os19.department = "humaniora"; os19.objekt = 43981000; orgsubjectlist.Add(os19);
            orgsubjectclass os20 = new orgsubjectclass(); os20.osname = "religionsvetenskap"; os20.academy = "HM"; os20.department = "humaniora"; os20.objekt = 43991000; orgsubjectlist.Add(os20);


            orgsubjectclass os23 = new orgsubjectclass(); os23.osname = "arbetsvetenskap"; os23.academy = "IoS"; os23.department = "avd 2"; os23.objekt = 46381000; orgsubjectlist.Add(os23);
            orgsubjectclass os24 = new orgsubjectclass(); os24.osname = "byggteknik"; os24.academy = "IoS"; os24.department = "avd 5"; os24.objekt = 46111000; orgsubjectlist.Add(os24);
            orgsubjectclass os25 = new orgsubjectclass(); os25.osname = "datateknik"; os25.academy = "IoS"; os25.department = "avd 3"; os25.objekt = 46221000; orgsubjectlist.Add(os25);
            orgsubjectclass os26 = new orgsubjectclass(); os26.osname = "energiteknik"; os26.academy = "IoS"; os26.department = "avd 5"; os26.objekt = 46121000; orgsubjectlist.Add(os26);
            orgsubjectclass os27 = new orgsubjectclass(); os27.osname = "entreprenörskap"; os27.academy = "IoS"; os27.department = "avd 1"; os27.objekt = 46301000; orgsubjectlist.Add(os27);
            orgsubjectclass os28 = new orgsubjectclass(); os28.osname = "fysik"; os28.academy = "IoS"; os28.department = "avd 4"; os28.objekt = 46621000; orgsubjectlist.Add(os28);
            orgsubjectclass os29 = new orgsubjectclass(); os29.osname = "företagsekonomi"; os29.academy = "IoS"; os29.department = "avd 1"; os29.objekt = 46311000; orgsubjectlist.Add(os29);
            orgsubjectclass os30 = new orgsubjectclass(); os30.osname = "grafisk teknologi"; os30.academy = "IoS"; os30.department = "avd 3"; os30.objekt = 46411000; orgsubjectlist.Add(os30);
            orgsubjectclass os31 = new orgsubjectclass(); os31.osname = "industriell ekonomi"; os31.academy = "IoS"; os31.department = "avd 1"; os31.objekt = 46321000; orgsubjectlist.Add(os31);
            orgsubjectclass os32 = new orgsubjectclass(); os32.osname = "informatik"; os32.academy = "IoS"; os32.department = "avd 3"; os32.objekt = 46231000; orgsubjectlist.Add(os32);
            orgsubjectclass os33 = new orgsubjectclass(); os33.osname = "kemiteknik"; os33.academy = "IoS"; os33.department = "avd 4"; os33.objekt = 46651000; orgsubjectlist.Add(os33);
            orgsubjectclass os34 = new orgsubjectclass(); os34.osname = "kulturgeografi"; os34.academy = "IoS"; os34.department = "avd 2"; os34.objekt = 46911000; orgsubjectlist.Add(os34);
            orgsubjectclass os35 = new orgsubjectclass(); os35.osname = "maskinteknik"; os35.academy = "IoS"; os35.department = "avd 4"; os35.objekt = 46511000; orgsubjectlist.Add(os35);
            orgsubjectclass os36 = new orgsubjectclass(); os36.osname = "matematik"; os36.academy = "IoS"; os36.department = "avd 4"; os36.objekt = 46631000; orgsubjectlist.Add(os36);
            orgsubjectclass os37 = new orgsubjectclass(); os37.osname = "materialteknik"; os37.academy = "IoS"; os37.department = "avd 4"; os37.objekt = 46521000; orgsubjectlist.Add(os37);
            orgsubjectclass os38 = new orgsubjectclass(); os38.osname = "mikrodata"; os38.academy = "IoS"; os38.department = "avd 3"; os38.objekt = 46391000; orgsubjectlist.Add(os38);
            orgsubjectclass os39 = new orgsubjectclass(); os39.osname = "nationalekonomi"; os39.academy = "IoS"; os39.department = "avd 2"; os39.objekt = 46331000; orgsubjectlist.Add(os39);
            orgsubjectclass os40 = new orgsubjectclass(); os40.osname = "pal"; os40.academy = "IoS"; os40.department = "avd 2"; os40.objekt = 46731000; orgsubjectlist.Add(os40);
            orgsubjectclass os41 = new orgsubjectclass(); os41.osname = "rättsvetenskap"; os41.academy = "IoS"; os41.department = "avd 1"; os41.objekt = 46721000; orgsubjectlist.Add(os41);
            orgsubjectclass os42 = new orgsubjectclass(); os42.osname = "samhällsbyggnadsteknik"; os42.academy = "IoS"; os42.department = "avd 5"; os42.objekt = 46371000; orgsubjectlist.Add(os42);
            orgsubjectclass os43 = new orgsubjectclass(); os43.osname = "skogsteknik"; os43.academy = "IoS"; os43.department = "avd 5"; os43.objekt = 46811000; orgsubjectlist.Add(os43);
            orgsubjectclass os44 = new orgsubjectclass(); os44.osname = "statistik"; os44.academy = "IoS"; os44.department = "avd 3"; os44.objekt = 46341000; orgsubjectlist.Add(os44);
            orgsubjectclass os45 = new orgsubjectclass(); os45.osname = "turism"; os45.academy = "IoS"; os45.department = "avd 2"; os45.objekt = 46921000; orgsubjectlist.Add(os45);

            orgsubjectclass os47 = new orgsubjectclass(); os47.osname = "sociologi"; os47.academy = "UHS"; os47.department = "samhälle och välfärd"; os47.objekt = 45751000; orgsubjectlist.Add(os47);
            orgsubjectclass os48 = new orgsubjectclass(); os48.osname = "geografi"; os48.academy = "UHS"; os48.department = "mange"; os48.objekt = 45351000; orgsubjectlist.Add(os48);
            orgsubjectclass os49 = new orgsubjectclass(); os49.osname = "idrotts- och hälsovetenskap"; os49.academy = "UHS"; os49.department = "idrott och medicin"; os49.objekt = 45691000; orgsubjectlist.Add(os49);
            orgsubjectclass os50 = new orgsubjectclass(); os50.osname = "matematikdidaktik"; os50.academy = "UHS"; os50.department = "mange"; os50.objekt = 45151000; orgsubjectlist.Add(os50);
            orgsubjectclass os51 = new orgsubjectclass(); os51.osname = "medicinsk vetenskap"; os51.academy = "UHS"; os51.department = "idrott och medicin"; os51.objekt = 45671000; orgsubjectlist.Add(os51);
            orgsubjectclass os52 = new orgsubjectclass(); os52.osname = "naturvetenskap"; os52.academy = "UHS"; os52.department = "mange"; os52.objekt = 45801000; orgsubjectlist.Add(os52);
            orgsubjectclass os53 = new orgsubjectclass(); os53.osname = "omvårdnad"; os53.academy = "UHS"; os53.department = "omvårdnad"; os53.objekt = 45681000; orgsubjectlist.Add(os53);
            orgsubjectclass os54 = new orgsubjectclass(); os54.osname = "pedagogiskt arbete"; os54.academy = "UHS"; os54.department = "utbildningsvetenskap"; os54.objekt = 45931000; orgsubjectlist.Add(os54);
            orgsubjectclass os55 = new orgsubjectclass(); os55.osname = "pedagogik"; os55.academy = "UHS"; os55.department = "utbildningsvetenskap"; os55.objekt = 45771000; orgsubjectlist.Add(os55);
            orgsubjectclass os56 = new orgsubjectclass(); os56.osname = "samhällskunskap"; os56.academy = "UHS"; os56.department = "samhälle och välfärd"; os56.objekt = 45791000; orgsubjectlist.Add(os56);
            orgsubjectclass os57 = new orgsubjectclass(); os57.osname = "socialt arbete"; os57.academy = "UHS"; os57.department = "samhälle och välfärd"; os57.objekt = 45781000; orgsubjectlist.Add(os57);
            orgsubjectclass os58 = new orgsubjectclass(); os58.osname = "srph"; os58.academy = "UHS"; os58.department = "omvårdnad"; os58.objekt = 45601000; orgsubjectlist.Add(os58);
            orgsubjectclass os59 = new orgsubjectclass(); os59.osname = "statsvetenskap"; os59.academy = "UHS"; os59.department = "samhälle och välfärd"; os59.objekt = 45761000; orgsubjectlist.Add(os59);

            using (StreamReader sr = new StreamReader(@"C:\dotnwb3\budgetdata\teachers.txt"))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] words = line.Split('\t');
                    if (words.Length < 2)
                        continue;
                    string sig = identify_teacher_name(words[0]);
                    if (sig == "###")
                    {
                        memo(words[0] + " " + words[1] + " unidentified");
                        continue;
                    }

                    var sigquery = from tc in teacherlist where tc.teacherID == sig select tc;
                    foreach (teacherclass tc in sigquery)
                    {
                        //memo(words[0] + "| "+tc.name + ": " + words[1]);
                        tc.subject = words[1].ToLower();
                    }
                }
            }
            //foreach (teacherclass tc in teacherlist)
            //{
            //    if (String.IsNullOrEmpty(tc.subject))
            //        memo("No subject "+tc.name);
            //}

            altsubjectdict.Add("afrikanska studier", "african studies");
            altsubjectdict.Add("arabiska", "arabiska");
            altsubjectdict.Add("arbetsvetenskap", "arbetsvetenskap");
            altsubjectdict.Add("bild", "bild");
            altsubjectdict.Add("bildproduktion", "bildproduktion");
            altsubjectdict.Add("bygg", "byggteknik");
            altsubjectdict.Add("datateknik", "datateknik");
            altsubjectdict.Add("energi", "energiteknik");
            altsubjectdict.Add("engelska", "engelska");
            altsubjectdict.Add("entreprenörskap", "entreprenörskap");
            altsubjectdict.Add("filosofi", "filosofi");
            altsubjectdict.Add("franska", "franska");
            altsubjectdict.Add("fysik", "fysik");
            altsubjectdict.Add("företagsekonomi", "företagsekonomi");
            altsubjectdict.Add("geografi", "geografi");
            altsubjectdict.Add("grafisk teknologi", "grafisk teknologi");
            altsubjectdict.Add("historia", "historia");
            altsubjectdict.Add("idrott och hälsa", "idrotts- och hälsovetenskap");
            altsubjectdict.Add("industriell ekonomi", "industriell ekonomi");
            altsubjectdict.Add("informatik", "informatik");
            altsubjectdict.Add("italienska", "italienska");
            altsubjectdict.Add("japanska", "japanska");
            altsubjectdict.Add("kemiteknik", "kemiteknik");
            altsubjectdict.Add("kinesiska", "kinesiska");
            altsubjectdict.Add("kulturgeografi", "kulturgeografi");
            altsubjectdict.Add("litteraturvetenskap", "litteraturvetenskap");
            altsubjectdict.Add("ljud- och musikproduktion", "ljud- och musikproduktion");
            altsubjectdict.Add("maskinteknik", "maskinteknik");
            altsubjectdict.Add("matematik", "matematik");
            altsubjectdict.Add("matematikdidaktik", "matematikdidaktik");
            altsubjectdict.Add("material", "materialteknik");
            altsubjectdict.Add("medicinsk vetenskap", "medicinsk vetenskap");
            altsubjectdict.Add("mikrodata", "mikrodata");
            altsubjectdict.Add("nationalekonomi", "nationalekonomi");
            altsubjectdict.Add("naturvetenskap", "naturvetenskap");
            altsubjectdict.Add("omvårdnad", "omvårdnad");
            altsubjectdict.Add("pedagogik", "pal");
            altsubjectdict.Add("pedagogiskt arbete", "pedagogik");
            altsubjectdict.Add("personal och arbetsliv", "pedagogiskt arbete");
            altsubjectdict.Add("portugisiska", "portugisiska");
            altsubjectdict.Add("religionsvetenskap", "religionsvetenskap");
            altsubjectdict.Add("ryska", "ryska");
            altsubjectdict.Add("rättsvetenskap", "rättsvetenskap");
            altsubjectdict.Add("samhällskunskap", "samhällskunskap");
            altsubjectdict.Add("sexuell reproduktiv perinatal hälsa", "srph");
            altsubjectdict.Add("skog", "skogsteknik");
            altsubjectdict.Add("socialt arbete", "socialt arbete");
            altsubjectdict.Add("sociologi", "sociologi");
            altsubjectdict.Add("spanska", "spanska");
            altsubjectdict.Add("statistik", "statistik");
            altsubjectdict.Add("statsvetenskap", "statsvetenskap");
            altsubjectdict.Add("svenska", "svenska");
            altsubjectdict.Add("svenska som andraspråk", "svenska som andraspråk");
            altsubjectdict.Add("turism", "turism");
            altsubjectdict.Add("tyska", "tyska");

        }

        private void button_nosig_Click(object sender, EventArgs e)
        {
            var nsquery =
                from tgs in tgslist
                where tgs.teacherID == "###"
                select tgs;
            memo("Lacking signature:");
            foreach (tgssheetclass tgs in nsquery)
                memo(tgs.teachername + "\t" + tgs.filename);
        }

        private void textBox1_ModifiedChanged(object sender, EventArgs e)
        {
            maxfiles = Convert.ToInt32(textBox1.Text);
            memo("Maxfiles = " + maxfiles.ToString());
            textBox1.Modified = false;
        }

        private void getacademicyear()
        {
            ht = htButton.Checked;
            int y = tryconvert(yearBox.Text);
            if (y > 100)
                y = y % 100;
            
            if (y >= 0)
            {
                tgsyear = y + 2000;
                if (ht)
                    academicyear = y.ToString() + "-" + (y + 1).ToString();
                else
                    academicyear = (y - 1).ToString() + "-" + y.ToString();
            }
            aclabel.Text = academicyear;
            yearBox.Modified = false;
            
        }

        private void vtButton_CheckedChanged(object sender, EventArgs e)
        {
            getacademicyear();
        }

        private void htButton_CheckedChanged(object sender, EventArgs e)
        {
            getacademicyear();
        }

        private void yearBox_ModifiedChanged(object sender, EventArgs e)
        {
            getacademicyear();
        }

        private string get_checked_academy()
        {
            if (radioHM.Checked)
                return "HM";
            if (radioUHS.Checked)
                return "UHS";
            if (radioIoS.Checked)
                return "IoS";
            return "";
        }

        private void radioHM_CheckedChanged(object sender, EventArgs e)
        {
            academy = get_checked_academy();
        }

        private void radioUHS_CheckedChanged(object sender, EventArgs e)
        {
            academy = get_checked_academy();
        }

        private void radioIoS_CheckedChanged(object sender, EventArgs e)
        {
            academy = get_checked_academy();
        }

        private void read_courseregfile(string f)
        {
            int courseID = courselist.Count + 1;

            int year = -1;
            int ncourse = 0;
            int nline = 0;
            using (StreamReader sr = new StreamReader(f))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    //memo(line);
                    nline++;
                    if ((nline == 2) || (line.IndexOf("År:") == 0))
                    {
                        year = tryconvert(line.Substring(4));
                        memo(year.ToString());
                    }
                    if (year < 0)
                        continue;
                    string[] words = line.Split('\t');
                    if (words.Length < 4)
                        continue;

                    List<string> ccodes = tgssheetclass.getcoursecode(words[1]);
                    if (ccodes.Count != 1)
                        continue;

                    //class courseclass
                    //{
                    //    public string coursecode = ""; //ladok-kod, PG1044 etc
                    //    public string applicationcode = ""; //anmälsningskod, V2JBF etc
                    //    public string name = "";
                    //    public bool ht = true; //false if vt
                    //    public int year = 2016;
                    //    public int ffgreg = 0; //antal förstagångregistrerade
                    //    public int paying = 0;  //betalande stud
                    //    public int exchange = 0; //utbytesstud inom avtal
                    //    public int dropout = 0;
                    //    public int earlydropout = 0;
                    //    public int age = 0; //average age
                    //    public int men = 0; //% men
                    //}

                    ncourse++;
                    courseID++;
                    courseclass cc = new courseclass();
                    cc.courseID = courseID;
                    cc.coursecode = ccodes.First();
                    cc.applicationcode = words[2];
                    if (String.IsNullOrEmpty(cc.applicationcode))
                        cc.applicationcode = "(anmkod saknas)";
                    cc.name = words[0].Replace(cc.coursecode, "").Replace(cc.applicationcode, "").Replace("( / )", "");
                    cc.year = year;
                    cc.ht = (cc.applicationcode[0] == 'H');
                    cc.ffgreg = tryconvert0(words[3]);
                    if (words.Length >= 10)
                    {
                        cc.paying = tryconvert0(words[4]);
                        cc.exchange = tryconvert0(words[5]);
                        cc.dropout = tryconvert0(words[6]);
                        cc.earlydropout = tryconvert0(words[7]);
                        cc.age = (float)tryconvertdouble(words[8]);
                        cc.men = (float)(0.01*tryconvertdouble(words[9]));
                        if ( cc.men < 0 )
                            cc.men = 0;
                    }
                    courselist.Add(cc);
                }
            }
            memo(ncourse.ToString() + " courses.");

        }

        private void read_courseactivefile(string f)
        {
            int year = -1;
            int ncourse = 0;
            int nfail = 0;
            int nline = 0;
            List<string> faillist = new List<string>();
            using (StreamReader sr = new StreamReader(f))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    //memo(line);
                    nline++;
                    if ((nline == 1) || (line.IndexOf("År:") == 0))
                    {
                        year = tryconvert(line.Substring(4));
                        memo(year.ToString());
                    }
                    if (nline % 1000 == 0)
                        memo(nline + " " + line);

                    if (year < 0)
                        continue;
                    string[] words = line.Split('\t');
                    if (words.Length < 4)
                        continue;

                    if (String.IsNullOrEmpty(words[0]))
                        continue;

                    List<string> ccodes = tgssheetclass.getcoursecode(words[1]);
                    string ccode = "prog";
                    if (ccodes.Count == 1)
                        ccode = ccodes.First();
                    else
                    {
                        if (words[8].Contains("paket"))
                            ccode = "paket";
                        else
                            ccode = "prog";
                    }

                    //class courseclass
                    //{
                    //    public string coursecode = ""; //ladok-kod, PG1044 etc
                    //    public string applicationcode = ""; //anmälsningskod, V2JBF etc
                    //    public string name = "";
                    //    public bool ht = true; //false if vt
                    //    public int year = 2016;
                    //    public int ffgreg = 0; //antal förstagångregistrerade
                    //    public int paying = 0;  //betalande stud
                    //    public int exchange = 0; //utbytesstud inom avtal
                    //    public int dropout = 0;
                    //    public int earlydropout = 0;
                    //    public int age = 0; //average age
                    //    public int men = 0; //% men
                    //    public double hp = 0;
                    //public bool distance = false;
                    //public string city = "Falun";
                    //public string dayevening = "DAG";
                    //public int startv = 0;
                    //public int slutv = 0;
                    //public string hptermin = "";
                    //public int fee = 0;

                    //}


                    var query = from course in courselist
                                where course.year == year
                                where course.coursecode == ccode
                                where course.applicationcode == words[0]
                                select course;
                    List<courseclass> cl = query.ToList();
                    if (cl.Count == 1)
                    {
                        courseclass cc = cl.First();
                        cc.hp = (float)tryconvertdouble(words[3]);
                        if (words.Length >= 17)
                        {
                            cc.hptermin = words[4];
                            cc.studyrate = tryconvert(words[5]);
                            cc.distance = (words[6].ToLower().Contains("istans"));
                            cc.city = words[11];
                            cc.dayevening = words[12];
                            if (cc.dayevening.Length > 5)
                                cc.dayevening = cc.dayevening.Substring(0, 5);
                            cc.startv = tryconvert(words[13]);
                            cc.slutv = tryconvert(words[14]);
                            cc.fee = tryconvert(words[16]);
                            cc.applied_as = words[10];
                            cc.language = words[7];
                        }
                        ncourse++;
                    }
                    else
                    {
                        //memo("cl.Count = " + cl.Count);
                        faillist.Add(line);
                        courseclass cc = new courseclass();
                        cc.year = year;
                        cc.name = words[2];
                        cc.coursecode = ccode;
                        cc.applicationcode = words[0];
                        cc.ht = (cc.applicationcode[0] == 'H');
                        cc.hp = (float)tryconvertdouble(words[3]);
                        cc.hptermin = words[4];
                        cc.studyrate = tryconvert(words[5]);
                        cc.distance = (words[6].ToLower().Contains("istans"));
                        cc.city = words[11];
                        cc.dayevening = words[12];
                        if (cc.dayevening.Length > 5)
                            cc.dayevening = cc.dayevening.Substring(0, 5);
                        cc.startv = tryconvert(words[13]);
                        cc.slutv = tryconvert(words[14]);
                        cc.fee = tryconvert(words[16]);
                        cc.applied_as = words[10];
                        cc.language = words[7];

                        courselist.Add(cc);
                        nfail++;
                    }
                    
                }
            }
            var qy = from course in courselist
                        where year == course.year
                        select course;
            //foreach (courseclass cc in qy)
            //    memo(cc.print());
            //foreach (string s in faillist)
            //    memo(s);
            memo(nfail.ToString() + " courses not found.");
            memo(ncourse.ToString() + " courses found.");

        }

        private void read_coursehstfile(string f)
        {
            int year = -1;
            int ncourse = 0;
            int nfail = 0;
            int nline = 0;
            List<string> faillist = new List<string>();


            using (StreamReader sr = new StreamReader(f))
            {
                sr.ReadLine();
                sr.ReadLine(); //throw away two header lines

                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    //memo(line);
                    nline++;
                    if (nline % 1000 == 0)
                        memo(nline + " " + line);
                    year = tryconvert(line.Substring(0,4));
                    //memo(year.ToString());
                    
                    if (year < 0)
                        continue;
                    string[] words = line.Split('\t');
                    if (words.Length < 9)
                        continue;

                    List<string> ccodes = tgssheetclass.getcoursecode(words[7]);
                    if (ccodes.Count != 1)
                        continue;

                    bool ht = (words[1][4] == '2');
                    //memo("ht = " + ht.ToString());


                    //class courseclass
                    //{
                    //    public string coursecode = ""; //ladok-kod, PG1044 etc
                    //    public string applicationcode = ""; //anmälsningskod, V2JBF etc
                    //    public string name = "";
                    //    public bool ht = true; //false if vt
                    //    public int year = 2016;
                    //    public int ffgreg = 0; //antal förstagångregistrerade
                    //    public int paying = 0;  //betalande stud
                    //    public int exchange = 0; //utbytesstud inom avtal
                    //    public int dropout = 0;
                    //    public int earlydropout = 0;
                    //    public int age = 0; //average age
                    //    public int men = 0; //% men
                    //    public double hp = 0;
                    //public bool distance = false;
                    //public string city = "Falun";
                    //public string dayevening = "DAG";
                    //public int startv = 0;
                    //public int slutv = 0;
                    //public string hptermin = "";
                    //public int fee = 0;

                    //}


                    var query = from course in courselist
                                where course.year == year
                                where course.coursecode == ccodes.First()
                                where course.ht == ht
                                select course;
                    List<courseclass> cl = query.ToList();
                    if (cl.Count > 0)
                    {
                        int fftot = 0;
                        foreach (courseclass cc in cl)
                            fftot += cc.ffgreg;
                        foreach (courseclass cc in cl)
                        {
                            float fraction = 1 / cl.Count;
                            if (fftot > 0)
                                fraction = cc.ffgreg / fftot;
                            float hst = (float)tryconvertdouble(words[4]);
                            float hpr = (float)tryconvertdouble(words[5]);
                            add_hsthpr(cc, hst*fraction, hpr*fraction, words[3]);
                            //cc.utb_omr = words[3];
                        }
                        ncourse++;
                    }
                    else
                    {
                        //memo("cl.Count = " + cl.Count);
                        faillist.Add(line);
                        nfail++;
                        courseclass cc = new courseclass();
                        cc.coursecode = ccodes.First();
                        cc.ht = ht;
                        cc.name = words[8];
                        cc.year = year;
                        float hst = (float)tryconvertdouble(words[4]);
                        float hpr = (float)tryconvertdouble(words[5]);
                        add_hsthpr(cc, hst, hpr, words[3]);
                        cl.Add(cc);
                    }

                }
            }
            memo(nfail.ToString() + " courses not found.");
            memo(ncourse.ToString() + " courses found.");

        }

        private void read_applicantfile(string f)
        {
            int year = getfileyear(f);
            int ncourse = 0;
            int nfail = 0;
            int nline = 0;
            List<string> faillist = new List<string>();


            using (StreamReader sr = new StreamReader(f))
            {
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine(); //throw away four header lines

                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    //memo(line);
                    nline++;
                    if (nline % 1000 == 0)
                        memo(nline + " " + line);


                    string[] words = line.Split('\t');
                    if (words.Length < 12)
                        continue;

                    List<string> applcodes = tgssheetclass.getapplcode(words[0]);
                    if (applcodes.Count != 1)
                        continue;

                    List<string> ccodes = tgssheetclass.getcoursecode(words[1]);

                    bool ht = (applcodes.First()[0] == 'H');
                    //memo("ht = " + ht.ToString());
                    
                    var query = from c in db.Course
                                where c.Applicationcode == applcodes.First()
                                where c.Year == year
                                where c.Ht == ht
                                select c;
                    List<Course> cl = query.ToList();
                    if ( cl.Count != 1)
                    {
                        nfail++;
                    }
                    else
                    {
                        ncourse++;
                        Course c = query.First();
                        c.Appl_tot = tryconvert0(words[2]);
                        c.Appl_1 = tryconvert0(words[3]);
                        c.Appl_1time = tryconvert0(words[4]);
                        c.Appl_1behorig = tryconvert0(words[5]);
                        c.Appl_totbehorig = tryconvert0(words[9]);
                        c.Appl_tottime = c.Appl_tot - tryconvert0(words[6]);
                        c.Appl_paying = tryconvert0(words[10]);
                        c.Appl_men = (float)(0.01*tryconvertdouble(words[11].Replace("%","")));
                        db.SubmitChanges();
                    }
                }
            }
            memo("read_applicantfile");
            memo(nfail.ToString() + " courses not found.");
            memo(ncourse.ToString() + " courses found.");

        }

        private void read_acceptedfile(string f)
        {
            int year = getfileyear(f);
            int ncourse = 0;
            int nfail = 0;
            int nline = 0;
            List<string> faillist = new List<string>();


            using (StreamReader sr = new StreamReader(f))
            {
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine(); //throw away four header lines

                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    //memo(line);
                    nline++;
                    if (nline % 1000 == 0)
                        memo(nline + " " + line);


                    string[] words = line.Split('\t');
                    if (words.Length < 10)
                        continue;

                    List<string> applcodes = tgssheetclass.getapplcode(words[0]);
                    if (applcodes.Count != 1)
                        continue;

                    List<string> ccodes = tgssheetclass.getcoursecode(words[1]);

                    bool ht = (applcodes.First()[0] == 'H');
                    //memo("ht = " + ht.ToString());

                    var query = from c in db.Course
                                where c.Applicationcode == applcodes.First()
                                where c.Year == year
                                where c.Ht == ht
                                select c;
                    List<Course> cl = query.ToList();
                    if (cl.Count != 1)
                    {
                        nfail++;
                    }
                    else
                    {
                        ncourse++;
                        Course c = query.First();
                        c.Accepted = tryconvert0(words[3]);
                        c.Reserves = tryconvert0(words[4]);
                        c.Accepted_1hand = tryconvert0(words[5]);
                        c.Accepted_late = tryconvert0(words[6]);
                        c.Accepted_paying = tryconvert0(words[7]);
                        db.SubmitChanges();
                    }
                }
            }
            memo("read_acceptedfile");
            memo(nfail.ToString() + " courses not found.");
            memo(ncourse.ToString() + " courses found.");

        }

        private void read_uhr_acceptedfile(string f)
        {
            int year = getfileyear(f);
            int ncourse = 0;
            int nfail = 0;
            int nline = 0;
            List<string> faillist = new List<string>();


            using (StreamReader sr = new StreamReader(f))
            {
                sr.ReadLine(); //throw away one header line

                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    //memo(line);
                    nline++;
                    if (nline % 1000 == 0)
                        memo(nline + " " + line);


                    string[] words = line.Split('\t');
                    if (words.Length < 10)
                        continue;

                    string applcode = words[3];
                    if (applcode.Contains("HDA-"))
                        applcode = applcode.Replace("HDA-", "");
                    else
                        continue;



                    bool ht = (applcode[0] == 'H');
                    //memo("ht = " + ht.ToString());

                    var query = from c in db.Course
                                where c.Applicationcode == applcode
                                where c.Year == year
                                where c.Ht == ht
                                select c;
                    List<Course> cl = query.ToList();
                    if (cl.Count != 1)
                    {
                        nfail++;
                    }
                    else
                    {
                        ncourse++;
                        Course c = query.First();
                        int acc = tryconvert0(words[5]) + tryconvert0(words[6]) + tryconvert0(words[7]);
                        int res = tryconvert0(words[8]) + tryconvert0(words[9]) + tryconvert0(words[10]);
                        if ( f.Contains("-u2-"))
                        {
                            c.Accepted_u2 = acc;
                            c.Reserves_u2 = res;
                        }
                        else
                        {
                            c.Accepted_u1 = acc;
                            c.Reserves_u1 = res;
                        }
                        db.SubmitChanges();
                    }
                }
            }
            memo("read_uhr_acceptedfile");
            memo(nfail.ToString() + " courses not found.");
            memo(ncourse.ToString() + " courses found.");

        }

        private void add_hsthpr(courseclass cc, float hst, float hpr, string utb_omr)
        {
            cc.hst += hst;
            cc.hpr += hpr;

            coursepengclass ccp = new coursepengclass();
            ccp.hst = hst;
            ccp.hpr = hpr;
            ccp.utb_omr = utb_omr.Substring(0, 2).ToUpper();
            if (utb_omr.Contains("edicin"))
                ccp.utb_omr = "MD";
            if (utb_omr.Contains("ndervisning"))
                ccp.utb_omr = "LU";
            cc.cplist.Add(ccp);

        }

        private void read_courseresultfile(string f)
        {
            int ncourse = 0;
            int nfail = 0;
            int nline = 0;
            List<string> faillist = new List<string>();


            using (StreamReader sr = new StreamReader(f))
            {
                sr.ReadLine(); //throw away one header line
                sr.ReadLine(); //throw away one header line
                sr.ReadLine(); //throw away one header line
                sr.ReadLine(); //throw away one header line

                string coursecode = "";
                string coursename = "";
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    //memo(line);
                    nline++;
                    if (nline % 1000 == 0)
                        memo(nline + " " + line);


                    string[] words = line.Split('\t');
                    if (words.Length < 7)
                        continue;

                    if (!String.IsNullOrEmpty(words[0].Trim()))
                        coursename = words[0];
                    if (!String.IsNullOrEmpty(words[1].Trim()))
                        coursecode = words[1];
                    int year = tryconvert(words[2]);
                    if (year < 0)
                        continue;

                    var query = from c in db.Course
                                where c.Coursecode == coursecode
                                where c.Year == year
                                select c;
                    List<Course> cl = query.ToList();
                    memo(coursename + "\t" + coursecode + "\t" + year + "\t" + cl.Count);
                    if (cl.Count == 0)
                    {
                        nfail++;
                    }
                    else
                    {
                        float ffgtot = (float)(from c in cl select c.Ffgreg).Sum();
                        if (ffgtot <= 0)
                            continue;
                        ncourse++;
                        int pass = tryconvert0(words[4]);
                        int somepass = tryconvert0(words[6]);
                        foreach (Course cc in query)
                        {
                            cc.Resultat_g = Convert.ToInt32(pass * cc.Ffgreg / ffgtot);
                            cc.Resultat_u = Convert.ToInt32(somepass * cc.Ffgreg / ffgtot);
                        }
                        db.SubmitChanges();
                    }
                }
            }
            memo("read_courseresultfile");
            memo(nfail.ToString() + " courses not found.");
            memo(ncourse.ToString() + " courses found.");

        }

        private bool samecourseinstance(Course cdb, courseclass cc)
        {
            if (cdb.Applicationcode != cc.applicationcode)
                return false;
            if (cdb.Year != cc.year)
                return false;
            if (cdb.Ht != cc.ht)
                return false;

            return true;
        }

        private void submit_changes(bool really_submit)
        {
            if (really_submit)
                db.SubmitChanges();
        }

        private void course_to_db()
        {
            memo("Emptying database");
            db.Course.DeleteAllOnSubmit(from c in db.Course where true select c);
            db.Coursepeng.DeleteAllOnSubmit(from c in db.Coursepeng where true select c);
            db.Batchentry.DeleteAllOnSubmit(from c in db.Batchentry where true select c);
            submit_changes(really_submit);

            memo("Filling database");
            List<int> cid = (from c in db.Course where true select c.CourseID).ToList();
            int nextid = 1;
            if (cid.Count > 0)
                nextid = cid.Max() + 1;

            int nextcpid = 1;
            List<int> cpid = (from c in db.Coursepeng where true select c.CpID).ToList();
            if (cpid.Count > 0)
                nextcpid = cpid.Max() + 1;

            //List<int> existingcc = (from c in db.Course where true select c.CourseID).ToList();
            List<string> existingcs = (from c in db.Coursesubject where true select c.SubjectID).ToList();
            foreach (courseclass cc in courselist)
            {
                var cquery = (from c in db.Course where c.Coursecode == cc.coursecode select c);
                bool found = false;
                foreach (Course cdb in cquery)
                {
                    if (samecourseinstance(cdb, cc))
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    memo("Skipping " + cc.coursecode);
                    continue;
                }

                Course cb = new Course();
                cb.CourseID = nextid;
                nextid++;
                cb.Coursecode = cc.coursecode;
                if (cc.coursecode.Length >= 2)
                {
                    cb.Subject = cc.coursecode.Substring(0, 2);
                    if (!existingcs.Contains(cb.Subject))
                    {
                        memo("Missing subject " + cb.Subject);
                        cb.Subject = null;
                    }
                }
                if (cc.applicationcode.Length != 5)
                {
                    memo("Bad applicationcode " + cc.applicationcode);
                    cb.Applicationcode = "?????";
                    continue;
                }
                else
                    cb.Applicationcode = cc.applicationcode;

                //SetAutoTruncate(cb,"Name",cc.name);
                if (cc.name.Length > 200)
                    cb.Name = cc.name.Substring(0, 200);
                else
                    cb.Name = cc.name;
                if (nextid % 100 == 1)
                    memo(nextid + ": " + cb.Coursecode + ": " + cb.Name);
                cb.Ht = cc.ht;
                cb.Year = cc.year;
                cb.Hp = (float)cc.hp;
                cb.Ffgreg = cc.ffgreg;
                cb.Paying = cc.paying;
                cb.Exchange = cc.exchange;
                cb.Dropout = cc.dropout;
                cb.Earlydropout = cc.earlydropout;
                cb.Age = (float)cc.age;
                cb.Men = (float)cc.men;
                cb.Hst = 0;
                cb.Hpr = 0;
                cb.Studyrate = cc.studyrate;
                cb.Distance = cc.distance;
                cb.City = cc.city;
                cb.Dayevening = cc.dayevening;
                cb.Startv = cc.startv.ToString();
                cb.Slutv = cc.slutv.ToString();
                cb.Hptermin = cc.hptermin.Replace(" ", "");
                if (cb.Hptermin.Length > 40)
                    cb.Hptermin = cb.Hptermin.Substring(0, 40);
                cb.Fee = cc.fee;
                if (!String.IsNullOrEmpty(cc.applied_as))
                    cb.Applied_as = cc.applied_as;
                else
                    cb.Applied_as = null;
                cb.Language = cc.language;
                languagehist.Add(cc.language);

                db.Course.InsertOnSubmit(cb);
                foreach (coursepengclass ccp in cc.cplist)
                {
                    Coursepeng cp = new Coursepeng();
                    cp.CpID = nextcpid;
                    nextcpid++;
                    cp.Peng = findpeng(ccp.utb_omr, cc.year);
                    if (cp.Peng > 0)
                    {
                        cp.Course = cb.CourseID;
                        cp.Hst = ccp.hst;
                        cp.Hpr = ccp.hpr;
                        db.Coursepeng.InsertOnSubmit(cp);
                    }

                }
                submit_changes(really_submit);

            }

            memo("Submitting...");
            submit_changes(really_submit);
            memo("Done!");

        }

        private int findpeng(string pengcode,int year)
        {

            var q1 = (from c in db.Studentpeng where c.Pengcode == pengcode select c);
            int defaultyear = 2016;
            int peng = -1;

            foreach ( Studentpeng c in q1)
            {
                if (c.Year == year) //if right year is found, return that...
                    return c.PengID;
                if (c.Year == defaultyear) //otherwise return 2016.
                    peng = c.PengID;
            }

            return peng;
        }

        private void make_batchentries() //match application codes with program batches and fill the db table batchentry with 
        {
            db.Batchentry.DeleteAllOnSubmit(from c in db.Batchentry where true select c);
            submit_changes(really_submit);

            for (int year = 2012; year < 2018; year++)
            {
                var qc = from c in db.Course 
                          where c.Coursecode == "prog" 
                          where c.Year == year 
                          select c;
                var qpb = from b in db.Programbatch 
                          where b.Startyear == year 
                          select b;
                List<int> qbe = (from e in db.Batchentry where true select e.BatchentryID).ToList();
                int nextbatchentry = qbe.Count + 1;
                while (qbe.Contains(nextbatchentry))
                    nextbatchentry++;

                foreach (Course c in qc)
                {
                    int batchmatch = -1;
                    if (batchentrydict.ContainsKey(c.Name))
                    {
                        var qpb2 = (from b in db.Programbatch where b.Startyear == year where b.Programtable.Name == batchentrydict[c.Name] select b);
                        int bmcount = 0;
                        foreach (Programbatch b in qpb2)
                        {
                            if (b.Ht != c.Ht)
                                continue;
                                                            //Accept long programs with variable length
                            if ((b.Programtable.Hp != c.Hp) && (b.Programtable.Hp < 270))
                                continue;
                            bmcount++;
                            batchmatch = b.ProgbatchID;
                        }
                        if (bmcount > 1)
                            memo("Too many matches at qbp2 " + bmcount);
                    }
                    else
                    {
                        string bestfrontmatch = "";
                        int bestfrontmatchcount = -1;
                        int bestfrontmatchid = -1;
                        string bestlevenmatch = "";
                        int bestlevenmatchcount = 999;
                        foreach (Programbatch b in qpb)
                        {
                            if (b.Ht != c.Ht)
                                continue;
                                                               //Accept long programs with variable length
                            if ((b.Programtable.Hp != c.Hp) && (b.Programtable.Hp < 270))
                                continue;
                            int mm = frontmatching(c.Name, b.Name);
                            if (mm > bestfrontmatchcount)
                            {
                                bestfrontmatch = b.Name;
                                bestfrontmatchcount = mm;
                                bestfrontmatchid = b.ProgbatchID;
                            }
                            int ll = LevenshteinDistance(c.Name, b.Name);
                            if (ll < bestlevenmatchcount)
                            {
                                bestlevenmatch = b.Name;
                                bestlevenmatchcount = ll;
                            }
                        }
                        if (bestlevenmatch == bestfrontmatch)
                        {
                            bestlevenmatch = "==";
                            batchmatch = bestfrontmatchid;
                        }
                        //memo(c.Name + "\t" + bestfrontmatch + "\t" + bestfrontmatchcount.ToString() + "\t" + bestlevenmatch + "\t" + bestlevenmatchcount.ToString());
                    }
                    if ( batchmatch >= 0)
                    {
                        Batchentry be = new Batchentry();
                        be.BatchentryID = nextbatchentry;
                        nextbatchentry++;
                        be.Programentry = c.CourseID;
                        be.Programbatch = batchmatch;
                        db.Batchentry.InsertOnSubmit(be);
                        submit_changes(really_submit);
                    }
                }
            }
        }

        private void courselistbutton_Click(object sender, EventArgs e)
        {
            int nfile = 0;
            List<string> filelist = get_filelist(@"C:\dotnwb3\kursdata\");
            foreach (string f in filelist)
            {
                memo(f);
                nfile++;
                if (f.Contains("Kurs_reg") && (f.Contains(".txt")))
                    read_courseregfile(f);
            }
            foreach (string f in filelist)
            {
                memo(f);
                nfile++;
                if (f.Contains("aktiva") && (f.Contains(".txt")))
                    read_courseactivefile(f);
            }
            foreach (string f in filelist)
            {
                memo(f);
                nfile++;
                if (f.Contains("HST och HPR") && (f.Contains(".txt")))
                    read_coursehstfile(f);
            }

            if (db != null)
            {
                course_to_db();
            }

            //make_batchentries();

            //memo("Printing language hist:");
            //memo(languagehist.GetSHist());
            //memo("Printing language hist done.");

            foreach (string f in filelist)
            {
                memo(f);
                nfile++;
                if (f.Contains("anmälan") && (f.Contains(".txt")))
                    read_applicantfile(f);
            }

            nfile = 0;
            foreach (string f in filelist)
            {
                memo(f);
                nfile++;
                if (f.Contains("antagning") && (f.Contains(".txt")))
                    read_acceptedfile(f);
            }

            //memo(nfile.ToString() + " files.");
            courselistbutton.Enabled = false;

            nfile = 0;
            List<string> filelist_uhr = get_filelist(@"C:\dotnwb3\uhr-data\");
            foreach (string f in filelist_uhr)
            {
                memo(f);
                nfile++;
                if (f.Contains("-u") && (f.Contains("-age.txt")))
                    read_uhr_acceptedfile(f);
            }

            memo(nfile.ToString() + " files.");
            courselistbutton.Enabled = false;

            read_courseresultfile(@"C:\dotnwb3\kursdata\genomstromning_tot.txt");

            //class courseclass
            //{
            //    public string coursecode = ""; //ladok-kod, PG1044 etc
            //    public string applicationcode = ""; //anmälsningskod, V2JBF etc
            //    public string name = "";
            //    public bool ht = true; //false if vt
            //    public int year = 2016;
            //    public int ffgreg = 0; //antal förstagångregistrerade
            //    public int paying = 0;  //betalande stud
            //    public int exchange = 0; //utbytesstud inom avtal
            //    public int dropout = 0;
            //    public int earlydropout = 0;
            //    public int age = 0; //average age
            //    public int men = 0; //% men
            //    public double hp = 0;
            //public bool distance = false;
            //public string city = "Falun";
            //public string dayevening = "DAG";
            //public int startv = 0;
            //public int slutv = 0;
            //public string hptermin = "";
            //public int fee = 0;

            //}

            //List<string> existingcs = (from c in db.Coursesubject where true select c.SubjectID).ToList();
            //nchange = 0;
            //foreach (string cc in subjectcodedict.Keys)
            //{
            //    if (existingcs.Contains(cc))
            //        continue;
            //    Coursesubject cs = new Coursesubject();
            //    cs.SubjectID = cc;
            //    cs.Name = subjectcodedict[cc];
            //    cs.Orgsubject = courseorgdict[cs.Name];
            //    db.Coursesubject.InsertOnSubmit(cs);
            //    nchange++;
            //}
            //if (nchange > 0)
            //    submit_changes(really_submit);
            //memo("nchange = " + nchange);
            //existingos = (from c in db.Orgsubject where true select c.OrgsubjectID).ToList();
            //memo("CourseSubjects in db after submit = " + existingos.Count);


        }

        //borrowed from https://www.codeproject.com/Articles/27392/Using-the-LINQ-ColumnAttribute-to-Get-Field-Length
        /// <span class="code-SummaryComment"><summary></span>
        /// Gets the length limit for a given field on a LINQ object ... or zero if not known
        /// <span class="code-SummaryComment"></summary></span>
        /// <span class="code-SummaryComment"><remarks></span>
        /// You can use the results from this method to dynamically 
        /// set the allowed length of an INPUT on your web page to
        /// exactly the same length as the length of the database column.  
        /// Change the database and the UI changes just by
        /// updating your DBML and recompiling.
        /// <span class="code-SummaryComment"></remarks></span>
        public static int GetLengthLimit(object obj, string field)
        {
            int dblenint = 0;   // default value = we can't determine the length

            Type type = obj.GetType();
            PropertyInfo prop = type.GetProperty(field);
            // Find the Linq 'Column' attribute
            // e.g. [Column(Storage="_FileName", DbType="NChar(256) NOT NULL", CanBeNull=false)]
            object[] info = prop.GetCustomAttributes(typeof(ColumnAttribute), true);
            // Assume there is just one
            if (info.Length == 1)
            {
                ColumnAttribute ca = (ColumnAttribute)info[0];
                string dbtype = ca.DbType;

                if (dbtype.StartsWith("NChar") || dbtype.StartsWith("NVarChar"))
                {
                    int index1 = dbtype.IndexOf("(");
                    int index2 = dbtype.IndexOf(")");
                    string dblen = dbtype.Substring(index1 + 1, index2 - index1 - 1);
                    int.TryParse(dblen, out dblenint);
                }
            }
            return dblenint;
        }

        /// <span class="code-SummaryComment"><summary></span>
        /// If you don't care about truncating data that you are setting on a LINQ object, 
        /// use something like this ...
        /// <span class="code-SummaryComment"></summary></span>
        public static void SetAutoTruncate(object obj, string field, string value)
        {
            int len = GetLengthLimit(obj, field);
            if (len == 0) throw new ApplicationException("Field '" + field +
                    "'does not have length metadata");

            Type type = obj.GetType();
            PropertyInfo prop = type.GetProperty(field);
            if (value.Length > len)
            {
                prop.SetValue(obj, value.Substring(0, len), null);
            }
            else
                prop.SetValue(obj, value, null);
        } 

        public static string replace_roman(string src)
        {
            return (src + " ").Replace(" i ", " 1 ").Replace(" ii ", " 2 ").Replace(" iii ", " 3 ").Trim();
        }

        private List<int> identify_course(tgsitemclass ti, int year, bool ht)
        {
            List<int> rl = new List<int>();

            int bestmatch = -1;
            int mindist = 999;
            int tolerance = 6;
            foreach (courseclass cc in courselist)
            {
                if (cc.ht != ht)
                    continue;
                if (cc.year != year)
                    continue;
                if (ti.coursecodes.Contains(cc.coursecode))
                {
                    rl.Add(cc.courseID);
                }
                //else
                //{
                if (String.IsNullOrEmpty(ti.label.Trim()))
                    continue;
                string ccname = cc.name.ToLower().Replace(",itd,", "").Replace(",nml", "");
                //p.text = Regex.Replace(p.text, replacepair.Key, replacepair.Value);
                ccname = Regex.Replace(ccname, @"1*\d\d%", "");
                    int dist = LevenshteinDistance(ccname, ti.label.Trim());
                    int distroman = LevenshteinDistance(cc.name.ToLower(), replace_roman(ti.label));
                    if (distroman < dist)
                        dist = distroman;
                    if (dist < mindist)
                    {
                        mindist = dist;
                        bestmatch = cc.courseID;
                    }
                //}
            }

            if ( rl.Count == 0)
            {
                course_bestmatchhist.Add(mindist);
                if ( mindist <= tolerance )
                {
                    rl.Add(bestmatch);
                    memo(mindist.ToString() + "\t" + ti.label + "\t" + (from cc in courselist where cc.courseID == bestmatch select cc).Single().name);
                }
            }
            else 
            {
                course_codematchhist.Add(mindist);
            }

            return rl;
        }

        public static int getfileprogsemester(string s) //assumes s contains "-Tx-" substring
        {
            

            //string regexcode = @"(-\d\d-&#124-\d{4}-)";
            string regexcode = @"-T\d-";

            Match m = Regex.Match(s, regexcode, RegexOptions.IgnoreCase);
            while (m.Success)
            {
                int i = tryconvert(m.Groups[0].Value.Trim('-').Replace("T",""));
                return i;
            }
            return -1;
        }

        public static int getfileyear(string s) //assumes s contains "-YY-" or "-YYYY-" substring
        {
            

            //string regexcode = @"(-\d\d-&#124-\d{4}-)";
            string regexcode = @"-\d\d-";

            Match m = Regex.Match(s, regexcode, RegexOptions.IgnoreCase);
            while (m.Success)
            {
                int i = tryconvert(m.Groups[0].Value.Trim('-'));
                if (i >= 0)
                {
                    if (i < 30)
                        return i + 2000;
                    else if (i < 100)
                        return i + 1900;
                    else
                        return i;
                }
            }

            regexcode = @"\d{4}-";
            m = Regex.Match(s, regexcode, RegexOptions.IgnoreCase);
            while (m.Success)
            {
                int i = tryconvert(m.Groups[0].Value.Trim('-'));
                if (i >= 0)
                {
                    if (i < 30)
                        return i + 2000;
                    else if (i < 100)
                        return i + 1900;
                    else
                        return i;
                }
            }

            return -1;
        }

        public static int frontmatching(string s1,string s2)
            //Count how far into the strings that s1 and s2 match
        {
            int minlen = Math.Min(s1.Length, s2.Length);
            char[] c1 = s1.ToCharArray();
            char[] c2 = s2.ToCharArray();
            for (int i = 0; i < minlen; i++)
            {
                if (c1[i] != c2[i])
                    return i;
            }
            return minlen;
        }

        public static int LevenshteinDistance(string src, string dest)
        {
            //From http://www.codeproject.com/Articles/36869/Fuzzy-Search
            //License CPOL (http://www.codeproject.com/info/cpol10.aspx)

            int[,] d = new int[src.Length + 1, dest.Length + 1];
            int i, j, cost;
            char[] str1 = src.ToCharArray();
            char[] str2 = dest.ToCharArray();

            for (i = 0; i <= str1.Length; i++)
            {
                d[i, 0] = i;
            }
            for (j = 0; j <= str2.Length; j++)
            {
                d[0, j] = j;
            }
            for (i = 1; i <= str1.Length; i++)
            {
                for (j = 1; j <= str2.Length; j++)
                {

                    if (str1[i - 1] == str2[j - 1])
                        cost = 0;
                    else
                        cost = 1;

                    d[i, j] =
                        Math.Min(
                            d[i - 1, j] + 1,              // Deletion
                            Math.Min(
                                d[i, j - 1] + 1,          // Insertion
                                d[i - 1, j - 1] + cost)); // Substitution

                    if ((i > 1) && (j > 1) && (str1[i - 1] ==
                        str2[j - 2]) && (str1[i - 2] == str2[j - 1]))
                    {
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
                    }
                }
            }

            return d[str1.Length, str2.Length];
        }



        private void outfilebutton_Click(object sender, EventArgs e)
        {
            int nfile = 0;
            List<string> filelist = get_filelist(@"C:\dotnwb3\TGStxt\");
            foreach (string f in filelist)
            {
                memo(f);
                nfile++;
                int year = getfileyear(f);
                bool htfile = f.Contains("HT");
                int nline = 0;
                using (StreamReader sr = new StreamReader(f))
                {
                    tgssheetclass currenttgs = new tgssheetclass();
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        //memo(line);
                        string[] words = line.Split('\t');
                        if (words.Length < 4)
                            continue;
                        if ( tryconvert(words[0]) >= 0)
                        {
                            tgsitemclass ti = new tgsitemclass();
                            bool ok = ti.read_print(words);
                            if (ok)
                                currenttgs.tgsitems.Add(ti);
                            //memo("adding item");
                        }
                        else
                        {
                            var sigquery = 
                                from teacher in teacherlist
                                where teacher.teacherID == words[1]
                                select teacher;
                            if (sigquery.ToList().Count() == 1)
                            {
                                if (!String.IsNullOrEmpty(currenttgs.teacherID))
                                {
                                    tgslist.Add(currenttgs);
                                    memo("Adding tgs for "+currenttgs.teachername);

                                }
                                currenttgs = new tgssheetclass();
                                currenttgs.read_dataline(words);
                            }
                        }

                    }
                    if (!String.IsNullOrEmpty(currenttgs.teacherID))
                        tgslist.Add(currenttgs);

                }
            }
            memo(nfile.ToString() + " files.");
            outfilebutton.Enabled = false;
            ReadTGSButton.Enabled = false;
            db_TGSbutton.Enabled = true;
        }

        private void db_addobjekt(long objekt,string academy,int verksamhet)
        {
            List<long> existing = (from c in db.Objekt where c.ObjektID == objekt select c.ObjektID).ToList();
            if (existing.Count > 0)
                return;
            Objekt oo = new Objekt();
            oo.ObjektID = objekt;
            oo.Budget_in = 0;
            oo.Budget_out = 0;
            oo.Academy = academy;
            oo.Verksamhet = verksamhet;
            db.Objekt.InsertOnSubmit(oo);
            submit_changes(really_submit);
        }

        private void db_addverksamhet(int verksamhet)
        {
            List<int> existing = (from c in db.Verksamhet where c.VerksamhetID == verksamhet select c.VerksamhetID).ToList();
            if (existing.Count > 0)
                return;
            Verksamhet vv = new Verksamhet();
            vv.VerksamhetID = verksamhet;
            vv.Label = "Okänd verksamhet";
            vv.First2 = verksamhet;
            while (vv.First2 > 100)
                vv.First2 = vv.First2 / 10;
            db.Verksamhet.InsertOnSubmit(vv);
            submit_changes(really_submit);
        }

        private void dbinitButton_Click(object sender, EventArgs e)
        {
            db = new DbTGSAnalysTest(connectionstring);


            //string[] aclist = { "HM", "IoS", "UHS", "???" };
            List<string> existingac = (from c in db.Academy where true select c.AcademyID).ToList();
            memo("Academies in db 1 = " + existingac.Count);
            int nchange = 0;
            if (existingac.Count < acdict.Count)
            {
                foreach (int iac in acdict.Keys)
                {
                    if (existingac.Contains(acdict[iac].acid))
                        continue;
                    Academy ac = new Academy();
                    ac.AcademyID = acdict[iac].acid;
                    ac.Label = acdict[iac].label;
                    ac.Number = iac;
                    db.Academy.InsertOnSubmit(ac);
                    nchange++;
                }
                existingac = (from c in db.Academy where true select c.AcademyID).ToList();
                memo("Academies in db before submit = " + existingac.Count);

                if (nchange > 0)
                    submit_changes(really_submit);

                existingac = (from c in db.Academy where true select c.AcademyID).ToList();
                memo("Academies in db after submit = " + existingac.Count);
            }

            List<int> existingvv = (from c in db.Verksamhet where true select c.VerksamhetID).ToList();
            using (StreamReader sr = new StreamReader(@"C:\dotnwb3\budgetdata\verksamhet.txt"))
            {
                nchange = 0;
                sr.ReadLine();//header line
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] words = line.Split('\t');
                    if (words.Length < 7)
                        continue;

                    Verksamhet vv = new Verksamhet();
                    vv.VerksamhetID = tryconvert(words[1]);
                    if (existingvv.Contains(vv.VerksamhetID))
                        continue;

                    vv.Label = words[2];
                    vv.First2 = tryconvert(words[7]);
                    db.Verksamhet.InsertOnSubmit(vv);
                    nchange++;
                }
                if (nchange > 0)
                    submit_changes(really_submit);
                memo(nchange + " verksamheter added to db.");
            }

            List<long> existingoo = (from c in db.Objekt where true select c.ObjektID).ToList();
            using (StreamReader sr = new StreamReader(@"C:\dotnwb3\budgetdata\objekt.txt"))
            {
                nchange = 0;
                sr.ReadLine();//header line
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] words = line.Split('\t');
                    if (words.Length < 5)
                        continue;

                    Objekt oo = new Objekt();
                    oo.ObjektID = tryconvertlong(words[1]);
                    if (existingoo.Contains(oo.ObjektID))
                        continue;

                    oo.Label = words[2];
                    oo.Verksamhet = tryconvert(words[3]);
                    if (!existingvv.Contains(oo.Verksamhet))
                    {
                        db_addverksamhet(oo.Verksamhet);
                    }
                    var aq = (from c in db.Academy where c.Label == words[5] select c.AcademyID);
                    if ( aq.Count() > 0)
                        oo.Academy = aq.First();
                    else 
                    {
                        memo(words[4] + " not found.");
                        continue;
                    }

                    oo.Subject = words[7];
                    db.Objekt.InsertOnSubmit(oo);
                    submit_changes(really_submit);
                    nchange++;
                }
                if (nchange > 0)
                    submit_changes(really_submit);
                memo(nchange + " objekt added to db.");

            }

            List<string> existingos = (from c in db.Orgsubject where true select c.OrgsubjectID).ToList();
            nchange = 0;
            foreach (orgsubjectclass osc in orgsubjectlist)
            {
                if (existingos.Contains(osc.osname))
                    continue;
                Orgsubject os = new Orgsubject();
                os.OrgsubjectID = osc.osname;
                os.Academy = osc.academy;
                os.Department = osc.department;
                os.Objekt = osc.objekt;
                db_addobjekt(osc.objekt,osc.academy,22);
                db.Orgsubject.InsertOnSubmit(os);
                nchange++;
            }
            if (nchange > 0)
                submit_changes(really_submit);
            memo("nchange = " + nchange);
            existingos = (from c in db.Orgsubject where true select c.OrgsubjectID).ToList();
            memo("OrgSubjects in db after submit = "+existingos.Count);

            List<string> existingcs = (from c in db.Coursesubject where true select c.SubjectID).ToList();
            nchange = 0;
            foreach (string cc in subjectcodedict.Keys)
            {
                if (existingcs.Contains(cc))
                    continue;
                Coursesubject cs = new Coursesubject();
                cs.SubjectID = cc;
                cs.Name = subjectcodedict[cc];
                cs.Orgsubject = courseorgdict[cs.Name];
                db.Coursesubject.InsertOnSubmit(cs);
                nchange++;
            }
            if (nchange > 0)
                submit_changes(really_submit);
            memo("nchange = " + nchange);
            existingos = (from c in db.Orgsubject where true select c.OrgsubjectID).ToList();
            memo("CourseSubjects in db after submit = " + existingos.Count);

            List<string> existingteachers = (from c in db.Teacher where true select c.TeacherID).ToList();
            nchange = 0;
            memo(teacherlist.Count + " teachers in teacherlist.");
            memo("Teachers in db before submit = " + existingteachers.Count);
            foreach (teacherclass tc in teacherlist) 
            {
                if (existingteachers.Contains(tc.teacherID))
                    continue;
                Teacher tt = new Teacher();
                tt.TeacherID = tc.teacherID;
                if (tt.TeacherID.Length > 4)
                {
                    tt.TeacherID = tt.TeacherID.Substring(0, 4);
                    if (existingteachers.Contains(tt.TeacherID))
                        continue;
                }
                tt.Firstname = tc.firstname;
                tt.Lastname = tc.lastname;
                tt.Name = tc.name;
                tt.Birthday = tc.birthday;

                tt.Subject = tc.subject.ToLower();
                if (String.IsNullOrEmpty(tt.Subject))
                    tt.Subject = "ämne saknas";
                if (!existingos.Contains(tt.Subject))
                {
                    if (altsubjectdict.ContainsKey(tt.Subject))
                        tt.Subject = altsubjectdict[tt.Subject];
                }
                
                db.Teacher.InsertOnSubmit(tt);
                //memo(tt.TeacherID+" submitted.");
                existingteachers.Add(tt.TeacherID);
                submit_changes(really_submit);
                nchange++;
            }
            if (nchange > 0)
                submit_changes(really_submit);
            memo("nchange = " + nchange);
            existingteachers = (from c in db.Teacher where true select c.TeacherID).ToList();
            memo("Teachers in db after submit = " + existingteachers.Count);

            int ipt = 1;
            if ((from c in db.Profileteacher select c.Id).Count() > 0)
            {
                ipt = (from c in db.Profileteacher select c.Id).Max() + 1;
                //db.Profileteacher.DeleteAllOnSubmit((from c in db.Profileteacher select c));
                //db.SubmitChanges();
            }

            string fn = @"C:\dotnwb3\budgetdata\forskare per profil.txt";

            using (StreamReader sr = new StreamReader(fn))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] words = line.Split('\t');
                    if (words.Length < 3)
                        continue;
                    if (!existingteachers.Contains(words[1]))
                    {
                        memo("Not found: " + line);
                        continue;
                    }
                    if ((from c in db.Profileteacher where c.Researchprofile == words[2] where c.Teacher == words[1] select c).Count() > 0)
                        continue;
                    Profileteacher pt = new Profileteacher();
                    pt.Id = ipt;
                    ipt++;
                    pt.Researchprofile = words[2];
                    pt.Teacher = words[1];
                    db.Profileteacher.InsertOnSubmit(pt);
                    db.SubmitChanges();
                }
            }


            int iau = 1;
            if ((from c in db.Author select c.Id).Count() > 0)
            {
                iau = (from c in db.Author select c.Id).Max() + 1;
                //db.Author.DeleteAllOnSubmit((from c in db.Author select c));
                //db.SubmitChanges();
            }

            string fn2 = @"C:\dotnwb3\budgetdata\Forskningspublikationer DiVA 2012-2017 csv 2.txt";

            using (StreamReader sr = new StreamReader(fn2))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] words = line.Split('\t');
                    if (words.Length < 3)
                        continue;
                    int did = tryconvert(words[0]);
                    if (did < 0)
                        continue;
                    string tid = words[2];
                    if (String.IsNullOrEmpty(tid))
                    {
                        string name = words[1];
                        string[] nameparts = name.Split(',');
                        string firstname = "";
                        if (nameparts.Length > 1)
                        {
                            name = nameparts[1].Trim() + " " + nameparts[0].Trim();
                            firstname = nameparts[1].Trim();
                        }
                        string lastname = nameparts[0].Trim();
                        tid = identify_teacher_name(firstname,lastname);
                        //memo(name + " " + tid);
                        if (String.IsNullOrEmpty(tid))
                            continue;
                        if (tid == "###")
                            continue;
                        if (tid.Length > 4)
                            continue;
                    }
                    if ((from c in db.Teacher where c.TeacherID == tid select c).Count() == 0)
                    {
                        memo(tid + " not found");
                        continue;
                    }

                    if ((from c in db.Author where c.Teacher == tid where c.DivaID == did select c).Count() > 0)
                        continue;
                    Author au = new Author();
                    au.Id = iau;
                    iau++;
                    au.Teacher = tid;
                    au.DivaID = did;
                    db.Author.InsertOnSubmit(au);
                    db.SubmitChanges();
                }
            }

            List<int> existingpeng = (from c in db.Studentpeng where true select c.PengID).ToList();
            nchange = 0;
            
            memo("Studentpeng in db before submit = " + existingpeng.Count);
            if (existingpeng.Count == 0)
            {
                Studentpeng sp1 = new Studentpeng(); sp1.Year = 2016; sp1.PengID = 1; sp1.Pengname = "Humaniora"; sp1.Pengcode = "HU"; sp1.Hstkr = 29981; sp1.Hprkr = 19609; sp1.Internal = true; db.Studentpeng.InsertOnSubmit(sp1);
                Studentpeng sp2 = new Studentpeng(); sp2.Year = 2016; sp2.PengID = 2; sp2.Pengname = "Idrott"; sp2.Pengcode = "ID"; sp2.Hstkr = 105575; sp2.Hprkr = 48944; sp2.Internal = true; db.Studentpeng.InsertOnSubmit(sp2);
                Studentpeng sp3 = new Studentpeng(); sp3.Year = 2016; sp3.PengID = 3; sp3.Pengname = "Juridik"; sp3.Pengcode = "JU"; sp3.Hstkr = 29981; sp3.Hprkr = 19609; sp3.Internal = true; db.Studentpeng.InsertOnSubmit(sp3);
                Studentpeng sp4 = new Studentpeng(); sp4.Year = 2016; sp4.PengID = 4; sp4.Pengname = "Undervisning"; sp4.Pengcode = "LU"; sp4.Hstkr = 36474; sp4.Hprkr = 38454; sp4.Internal = true; db.Studentpeng.InsertOnSubmit(sp4);
                Studentpeng sp5 = new Studentpeng(); sp5.Year = 2016; sp5.PengID = 5; sp5.Pengname = "Medicin"; sp5.Pengcode = "MD"; sp5.Hstkr = 60847; sp5.Hprkr = 74109; sp5.Internal = true; db.Studentpeng.InsertOnSubmit(sp5);
                Studentpeng sp6 = new Studentpeng(); sp6.Year = 2016; sp6.PengID = 6; sp6.Pengname = "Media"; sp6.Pengcode = "ME"; sp6.Hstkr = 294653; sp6.Hprkr = 238360; sp6.Internal = true; db.Studentpeng.InsertOnSubmit(sp6);
                Studentpeng sp7 = new Studentpeng(); sp7.Year = 2016; sp7.PengID = 7; sp7.Pengname = "Naturvetenskap"; sp7.Pengcode = "NA"; sp7.Hstkr = 51314; sp7.Hprkr = 43274; sp7.Internal = true; db.Studentpeng.InsertOnSubmit(sp7);
                Studentpeng sp8 = new Studentpeng(); sp8.Year = 2016; sp8.PengID = 8; sp8.Pengname = "Samhällsvetenskap"; sp8.Pengcode = "SA"; sp8.Hstkr = 29981; sp8.Hprkr = 19609; sp8.Internal = true; db.Studentpeng.InsertOnSubmit(sp8);
                Studentpeng sp9 = new Studentpeng(); sp9.Year = 2016; sp9.PengID = 9; sp9.Pengname = "Teknik"; sp9.Pengcode = "TE"; sp9.Hstkr = 51314; sp9.Hprkr = 43274; sp9.Internal = true; db.Studentpeng.InsertOnSubmit(sp9);
                Studentpeng sp10 = new Studentpeng(); sp10.Year = 2016; sp10.PengID = 10; sp10.Pengname = "Vård"; sp10.Pengcode = "VÅ"; sp10.Hstkr = 54450; sp10.Hprkr = 47168; sp10.Internal = true; db.Studentpeng.InsertOnSubmit(sp10);
                Studentpeng sp11 = new Studentpeng(); sp11.Year = 2016; sp11.PengID = 11; sp11.Pengname = "Övrigt"; sp11.Pengcode = "ÖV"; sp11.Hstkr = 41209; sp11.Hprkr = 33476; sp11.Internal = true; db.Studentpeng.InsertOnSubmit(sp11);
                Studentpeng sp12 = new Studentpeng(); sp12.Year = 2016; sp12.PengID = 12; sp12.Pengname = "VFU"; sp12.Pengcode = "VF"; sp12.Hstkr = 51711; sp12.Hprkr = 50441; sp12.Internal = true; db.Studentpeng.InsertOnSubmit(sp12);
                submit_changes(really_submit);
            }

            memo("DONE");
        }

        private long get_objekt_from_course(string coursecode)
        {
            if (coursecode.Length < 2)
                return -1;

            string cs = coursecode.Substring(0, 2);
            var qcs = (from c in db.Coursesubject where c.SubjectID == cs select c);
            if (qcs.Count() > 0)
            {
                var qos = (from c in db.Orgsubject where c.OrgsubjectID == qcs.First().Orgsubject select c);
                if (qos.Count() > 0)
                    return (long)qos.First().Objekt;
            }

            return -1;
            
        }

        private int get_ffgreg(string coursecode, int year, bool ht)
        {
            int ff = 0;
            foreach (Course c in (from c in db.Course 
                                  where c.Coursecode == coursecode
                                  where c.Year == year
                                  where c.Ht == ht
                                  select c))
            {
                ff += (int)c.Ffgreg;
                
            }

            return ff;
        }

        private float get_regtimeshp(string coursecode, int year, bool ht)
        {
            float? ff = 0;
            foreach (Course c in (from c in db.Course
                                  where c.Coursecode == coursecode
                                  where c.Year == year
                                  where c.Ht == ht
                                  select c))
            {
                ff += c.Ffgreg*c.Hp;
            }

            if (ff == null)
                return 0;
            else
                return (float)ff;
        }

        private int new_courseID(string coursecode, int year, bool ht, string label)
        {
            List<int> cid = (from c in db.Course where true select c.CourseID).ToList();
            int nextid = 1;
            if (cid.Count > 0)
                nextid = cid.Max() + 1;
            Course cc = new Course();
            cc.CourseID = nextid;
            cc.Year = year;
            cc.Ht = ht;
            cc.Name = label;
            db.Course.InsertOnSubmit(cc);
            submit_changes(really_submit);
            return nextid;
        }

        private int get_courseID(string coursecode, int year, bool ht)
        {
            return get_courseID(coursecode, year, ht, year);
        }
        private int get_courseID(string coursecode, int year, bool ht, int origyear)
        {
            var q =            (from c in db.Course
                                  where c.Coursecode == coursecode
                                  where c.Year == year
                                  where c.Ht == ht
                                  select c.CourseID);
            //memo(coursecode + " q.Count = "+q.Count());
            if ( q.Count() > 0)
            {
                return q.First();
            }
            else if ( origyear-year < 3)
            {
                if (ht)
                    return get_courseID(coursecode, year, false,origyear);
                else
                    return get_courseID(coursecode, year - 1, true,origyear);
            }
            else
                return -1;
        }

        private void db_TGSbutton_Click(object sender, EventArgs e)
        {
            if (db == null)
            {
                memo("Database not connected.");
                return;
            }

            memo("Emptying database");
            db.CourseTGS.DeleteAllOnSubmit(from c in db.CourseTGS where true select c);
            db.TGSitem.DeleteAllOnSubmit(from c in db.TGSitem where true select c);
            db.TGS.DeleteAllOnSubmit(from c in db.TGS where true select c);
            submit_changes(really_submit);

            memo("Filling database");


            List<int> existingtgs = (from t in db.TGS where true select t.TGSID).ToList();
            int nexttgs = 1;
            if (existingtgs.Count > 0)
                nexttgs = existingtgs.Max() + 1;
            List<int> existingitems = (from t in db.TGSitem where true select t.TGSitemID).ToList();
            int nextitem = 1;
            if (existingitems.Count > 0)
                nextitem = existingitems.Max() + 1;
            List<int> existingctgs = (from t in db.CourseTGS where true select t.CtgsID).ToList();
            int nextctgs = 1;
            if (existingctgs.Count > 0)
                nextitem = existingctgs.Max() + 1;

            memo("Adding TGS to database " + tgslist.Count);

            foreach (tgssheetclass tgs in tgslist)
            {
                if (tgs.best)
                {
                    var q1 = (from t in db.TGS
                              where t.Teacher == tgs.teacherID
                              where t.Year == tgs.year()
                              where t.Ht == tgs.is_ht
                              select t);
                    if ( q1.Count() > 0) //exists already. Update?
                    {
                        memo(tgs.teacherID + " exists already.");
                    }
                    else //new tgs
                    {
                        TGS tt = new TGS();
                        tt.TGSID = nexttgs;
                        nexttgs++;

                        tt.Teacher = tgs.teacherID;
                        tt.Ht = tgs.is_ht;
                        tt.Year = tgs.year();
                        tt.Totaltodo = (float)tgs.totaltodo;
                        tt.Tjlsjuk1 = (float)tgs.tjlsjuk1;
                        tt.Tjlsjuk2 = (float)tgs.tjlsjuk2;
                        tt.In_sparadsem = (float)tgs.in_sparadsem;
                        tt.Ut_sparadsem = (float)tgs.ut_sparadsem;
                        tt.Remainstodo = (float)tgs.remainstodo;
                        tt.Totaldone = (float)tgs.totaldone;
                        tt.Overunder = (float)tgs.overunder;
                        tt.Adjustment = (float)tgs.adjustment;
                        tt.Definitive = tgs.definitive;
                        tt.Filename = tgs.filename;
                        tt.Modified = tgs.modified;
                        memo("Adding to db " + tt.Teacher);
                        db.TGS.InsertOnSubmit(tt);
                        submit_changes(really_submit);

                        foreach (tgsitemclass ti in tgs.tgsitems)
                        {
                            TGSitem tti = new TGSitem();
                            tti.TGSitemID = nextitem;
                            nextitem++;

                            tti.Tgs = tt.TGSID;
                            tti.Hours = (float)ti.hours;
                            tti.Category = ti.category;
                            tti.Label = ti.label;
                            if (ti.label.Length > 100)
                                tti.Label = ti.label.Substring(0, 100);
                            int ncourse = ti.coursecodes.Count;
                            if (ti.objekt > 0)
                                tti.Objekt = ti.objekt;
                            else if ( ncourse > 0)
                            {
                                tti.Objekt = get_objekt_from_course(ti.coursecodes.First());
                            }
                            else
                                tti.Objekt = null;

                            if (tti.Objekt != null)
                                if ((from oo in db.Objekt where oo.ObjektID == tti.Objekt select oo).Count() == 0)
                                {
                                    Objekt oo = new Objekt();
                                    oo.ObjektID = (long)tti.Objekt;
                                    long acn = oo.ObjektID;
                                    while (acn > 100)
                                        acn = acn / 10;
                                    var qac = (from c in db.Academy where c.Number == acn select c.AcademyID).FirstOrDefault();
                                    if (qac != null)
                                        oo.Academy = qac;
                                    else
                                        oo.Academy = "???";
                                    oo.Verksamhet = 2;
                                    db.Objekt.InsertOnSubmit(oo);
                                    submit_changes(really_submit);
                                }

                            db.TGSitem.InsertOnSubmit(tti);
                            submit_changes(really_submit);


                            if ( ncourse > 0 )
                            {
                                float fftot = 0;
                                foreach (string cc in ti.coursecodes)
                                    fftot += get_regtimeshp(cc, (int)tt.Year, tgs.is_ht);
                                foreach (string cc in ti.coursecodes)
                                {
                                    float fraction = 1;
                                    if (ncourse > 1)
                                    {
                                        fraction = (float)1.0 / (float)ncourse;
                                        if (fftot > 0)
                                            fraction = get_regtimeshp(cc, (int)tt.Year, tgs.is_ht) / fftot;
                                    }
                                    CourseTGS ct = new CourseTGS();
                                    ct.CtgsID = nextctgs;
                                    nextctgs++;
                                    ct.Tgsitem = tti.TGSitemID;
                                    ct.Course = get_courseID(cc, (int)tt.Year, tgs.is_ht);
                                    if (ct.Course < 0)
                                    {
                                        ct.Course = new_courseID(cc, (int)tt.Year, tgs.is_ht, ti.label);
                                    }
                                    ct.Fraction = fraction;
                                    db.CourseTGS.InsertOnSubmit(ct);
                                    submit_changes(really_submit);
                                }
                            }
                        }
                        submit_changes(really_submit);
                    }
                        
                }
            }
            memo("Done!");
        }

        private void yearBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void makeprogcourse(string progcode, string coursecode)
        {
            int progcourseid = -1;
            string maincode = progcode;
            if (progcodedict.ContainsKey(progcode))
                maincode = progcodedict[progcode];
            if (!progsubjdict.ContainsKey(maincode))
            {
                memo("**************** Bad progcode " + maincode);
                return;
            }
            Programtable ppq = (from c in db.Programtable where c.Progcode == maincode select c).FirstOrDefault();
            if (ppq == null)
            {
                memo("Program not found " + progcode);
                return;
            }

            int progid = ppq.ProgID;

            var pcq = (from c in db.Programcourse where c.Program == progid where c.Coursecode == coursecode select c).FirstOrDefault();
            if ( pcq != null)
            {
                memo("Already in db " + progcode + " " + coursecode);
                return;
            }

            int pcid = 1;
            if ((from c in db.Programcourse select c).Count() > 0)
                pcid = (from c in db.Programcourse select c.Id).Max() + 1;

            Programcourse pc = new Programcourse();
            pc.Id = pcid;
            pc.Program = progid;
            pc.Coursecode = coursecode;
            db.Programcourse.InsertOnSubmit(pc);
            db.SubmitChanges();
        }

        private int makeprogbatch(string name,float hp,string progcode,int startyear, bool ht)
        {
            int progbatchid = -1;
            string maincode = progcode;
            if (progcodedict.ContainsKey(progcode))
                maincode = progcodedict[progcode];

            if ( !progsubjdict.ContainsKey(maincode))
            {
                memo("**************** Bad progcode " + maincode + "\t" + name);
                return -1;
            }

            var pbq = (from c in db.Programbatch where c.Progcode == progcode where c.Startyear == startyear where c.Ht == ht select c.ProgbatchID).ToList();
            if (pbq.Count() > 0)
                return pbq.First();
            else
            {
                pbq = (from c in db.Programbatch where true select c.ProgbatchID).ToList();
                progbatchid = pbq.Count+1;
                while (pbq.Contains(progbatchid))
                    progbatchid++;

                Programbatch pb = new Programbatch();
                pb.ProgbatchID = progbatchid;
                pb.Name = name;
                pb.Progcode = progcode;
                pb.Startyear = startyear;
                pb.Ht = ht;
                var ppq = (from c in db.Programtable where c.Progcode == maincode select c.ProgID).ToList();
                if (ppq.Count > 0)
                    pb.Program = ppq.First();
                else
                {
                    //make program
                    ppq = (from c in db.Programtable where true select c.ProgID).ToList();
                    int progid = ppq.Count + 1;
                    while (ppq.Contains(progid))
                        progid++;
                    Programtable pg = new Programtable();
                    pg.ProgID = progid;
                    pg.Name = name;
                    pg.Hp = hp;
                    pg.Progcode = maincode;
                    pg.Orgsubject = progsubjdict[maincode];
                    pg.Level = progleveldict[maincode];
                    db.Programtable.InsertOnSubmit(pg);
                    db.SubmitChanges();

                    pb.Program = progid;
                }
                db.Programbatch.InsertOnSubmit(pb);
            }
            db.SubmitChanges();

            return progbatchid;
        }

        private void read_progregfile(string filename)
        {
            int nchange = 0;

            int year = getfileyear(filename);
            bool ht = filename.Contains("-ht");
            int semester = getfileprogsemester(filename);
            int startyear = year;
            bool startht = ht;
            if (semester > 1)
            {
                int isem = semester;
                while (isem > 1)
                {
                    if (startht)
                        startht = false;
                    else
                    {
                        startht = true;
                        startyear--;
                    }
                    isem--;
                }
            }

            using (StreamReader sr = new StreamReader(filename))
            {
                nchange = 0;
                sr.ReadLine();//header line
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] words = line.Split('\t');
                    if (words.Length < 8)
                        continue;
                    
                    string progcode = "";
                    foreach (string c in tgssheetclass.getapplcode(words[0]))
                        progcode = c;
                    if (String.IsNullOrEmpty(progcode))
                        continue;

                    float hp = -1;
                    foreach (float c in tgssheetclass.gethp(words[0]))
                        hp = c;
                    if (hp < 0)
                        continue;
                    //memo(words[0] + "\t" + progcode + "\t" + hp.ToString());

                    int proglength = (int)Math.Ceiling((hp-2)/ 30);

                    int progbatchid = makeprogbatch(words[0], hp, progcode, startyear, startht);
                    if (progbatchid < 0)
                        continue;

                    Programsemester ps;
                    var qps = (from c in db.Programsemester where c.Programbatch == progbatchid where c.Semester == semester select c);
                    if (qps.Count() > 0)
                    {
                        ps = qps.First();
                        ps.Ffgreg = tryconvert0(words[1]);
                        ps.Omreg = tryconvert0(words[2]);
                        ps.Interrupts = tryconvert0(words[5]);
                        ps.Age = (float)tryconvertdouble(words[6]);
                        ps.Men = (float)(0.01 * tryconvertdouble(words[7].Replace("%", "")));
                    }
                    else
                    {
                        ps = new Programsemester();
                        List<int> qps2 = (from c in db.Programsemester where true select c.ProgsemID).ToList();
                        int psid = qps2.Count;
                        while (qps2.Contains(psid))
                            psid++;
                        ps.ProgsemID = psid;
                        ps.Programbatch = progbatchid;
                        ps.Semester = semester;
                        ps.Ffgreg = tryconvert0(words[1]);
                        ps.Omreg = tryconvert0(words[2]);
                        ps.Interrupts = tryconvert0(words[5]);
                        ps.Age = (float)tryconvertdouble(words[6]);
                        ps.Men = (float)(0.01 * tryconvertdouble(words[7].Replace("%", "")));
                        db.Programsemester.InsertOnSubmit(ps);
                    }
                    db.SubmitChanges();
                    nchange++;


                }
            }

        }

        private void proglistbutton_Click(object sender, EventArgs e)
        {
            int nfile = 0;
            List<string> filelist = get_filelist(@"C:\dotnwb3\kursdata\");
            foreach (string f in filelist)
            {
                memo(f);
                nfile++;
                if (f.Contains("progreg-linnea") && (f.Contains(".txt")))
                    read_progregfile(f);
            }


        }

        private void batchentry_button_Click(object sender, EventArgs e)
        {
            make_batchentries();
        }

        private void LakanButton_Click(object sender, EventArgs e)
        {
            string folder = @"C:\dotnwb3\TGS\IoS\Lakan\";
            List<string> skipwords = new List<string>();
            skipwords.Add("procent");
            skipwords.Add("sum research");
            skipwords.Add("summa");
            skipwords.Add("total");
            skipwords.Add("saldo");
            skipwords.Add("teaching");

            Excel.Application xlApp = new Excel.Application();
            using (StreamReader srl = new StreamReader(folder + "lakanstruktur.txt"))
            {
                srl.ReadLine(); //throw away header
                while (!srl.EndOfStream)
                {
                    string line = srl.ReadLine();
                    memo(line);
                    string[] words = line.Split('\t');
                    string subject = words[0];
                    int year = tryconvert(words[1]);
                    bool ht = (words[2] == "ht");
                    string academicyear;
                    if (ht)
                        academicyear = year.ToString().Substring(2, 2) + "/" + (year+1).ToString().Substring(2, 2);
                    else
                        academicyear = (year-1).ToString().Substring(2, 2) + "/" + year.ToString().Substring(2, 2);

                    string filename = folder+words[3];
                    int tab = tryconvert(words[4]);
                    int rteacher = tryconvert(words[5]);
                    int ccode = tryconvert(words[6]);
                    int cname = tryconvert(words[7]);
                    int ct1 = tryconvert(words[8]);
                    int pitch = tryconvert(words[9]);
                    int offset = tryconvert(words[10]);
                    int ctfinal = tryconvert(words[11]);
                    int r1 = tryconvert(words[12]);
                    int rfinal = tryconvert(words[13]);

                    memo(filename);
                    Excel.Workbook xlWorkBook;
                    Excel.Worksheet xll;


                    if (!File.Exists(filename))
                    {
                        memo("File not found!");
                        return;
                    }


                    xlWorkBook = xlApp.Workbooks.Open(filename, 0, true, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);

                    xll = xlWorkBook.Sheets[tab];

                    Dictionary<string, tgssheetclass> tsdict = new Dictionary<string, tgssheetclass>();
                    Dictionary<int, string> cteacherdict = new Dictionary<int, string>();

                    for (int i = ct1; i <= ctfinal;i += pitch )
                    {
                        string tname = tgssheetclass.getstring(xll.Cells[rteacher, i]);
                        string tsig = identify_teacher_name(tname);
                        if (tsig == "###")
                        {
                            memo("Teacher not found " + tname);
                            continue;
                        }
                        else
                            memo(tsig);
                        cteacherdict.Add(i, tsig);
                        tgssheetclass ts = new tgssheetclass();
                        ts.teachername = tname;
                        ts.teacherID = tsig;
                        ts.academicyear = academicyear;
                        ts.is_ht = ht;
                        ts.filename = filename;
                        ts.best = true;

                        tsdict.Add(tsig, ts);
                        memo("Adding tgs for " + tsig);
                    }

                    for (int j = r1; j <= rfinal;j++ )
                    {
                        string label = tgssheetclass.getstring(xll.Cells[j, cname]);
                        memo("Row " + j + ": " + label);
                        
                        if ((from c in skipwords where label.ToLower().IndexOf(c) == 0 select c).Count() > 0)
                            continue;
                        List<string> coursecodes;
                        if (ccode > 0)
                            coursecodes = tgssheetclass.getcoursecode(tgssheetclass.getstring(xll.Cells[j, ccode]));
                        else
                            coursecodes = new List<string>();
                        for (int i = ct1; i <= ctfinal;i += pitch )
                        {
                            string tsig = "";
                            if (cteacherdict.ContainsKey(i))
                                tsig = cteacherdict[i];
                            else
                                continue;

                            double klt = tgssheetclass.getdouble(xll.Cells[j, i+offset].Value);
                            if ( klt > 0)
                            {
                                tgsitemclass ti = new tgsitemclass();
                                ti.hours = klt;
                                if (coursecodes.Count > 0)
                                    ti.category = 0;
                                else
                                    ti.category = -1;
                                ti.label = label;
                                foreach (string cc in coursecodes)
                                    ti.coursecodes.Add(tgssheetclass.standardcoursecode(cc));
                                tsdict[tsig].tgsitems.Add(ti);
                                memo("Adding tgs item for " + tsig);
                            }
                        }

                    }

                    Marshal.ReleaseComObject(xll);
                    Marshal.ReleaseComObject(xlWorkBook);
                    memo("DONE reading "+filename);

                    string outfile = @"C:\dotnwb3\out-" + subject + "-" + getsemesterstring(year, ht) + "-" + getdatestring() + ".txt";
                    memo("Writing to " + outfile);
                    using (StreamWriter sw = new StreamWriter(outfile))
                    {
                        sw.WriteLine(tgssheetclass.tableheader());
                        foreach (tgssheetclass tgs in tsdict.Values)
                        {
                            if (tgs.best)
                            {
                                sw.WriteLine(tgs.dataline());
                                foreach (tgsitemclass ti in tgs.tgsitems)
                                    sw.WriteLine(ti.print());
                            }
                        }
                    }

                }
            }
            Marshal.ReleaseComObject(xlApp);
            memo("DONE!");
        }

        private void courseprogram_Click(object sender, EventArgs e)
        {
            read_courseprogramfile(@"C:\dotnwb3\kursdata\kurser till program.txt");
            read_courseprogramfile(@"C:\dotnwb3\kursdata\Kurser i program utan huvomr.txt");
        }

        private void read_courseprogramfile(string filename)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                int nchange = 0;
                sr.ReadLine();//header line
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] words = line.Split('\t');
                    if (words.Length < 5)
                        continue;

                    string progcode = words[3];
                    if (progcode.Length != 5)
                        continue;
                    string coursecode = tgssheetclass.standardcoursecode(words[0]);
                    if (coursecode.Length != 6)
                        continue;

                    string huvudomr = words[2];
                    if ( huvudomr.Length == 5)
                    {
                        var qc = (from c in db.Course where c.Coursecode == coursecode select c);
                        foreach (Course cc in qc)
                        {
                            if (cc.Huvudomr == null)
                                cc.Huvudomr = huvudomr;
                        }
                    }

                    makeprogcourse(progcode, coursecode);

                    nchange++;


                }
            }

        }

    }
}
