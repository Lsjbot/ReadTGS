using System;
using System.Collections.Generic;

public class hbookclass
{
    private SortedDictionary<string, int> shist = new SortedDictionary<string, int>();
    private SortedDictionary<int, int> ihist = new SortedDictionary<int, int>();
    private SortedDictionary<double, int> dhist = new SortedDictionary<double, int>();

    private const int MAXBINS = 202;
    private double[] binlimits = new double[MAXBINS];
    private double binmax = 100;
    private double binmin = 0;
    private double binwid = 0;
    private int nbins = MAXBINS - 2;

    public void Add(string key)
    {
        if (!shist.ContainsKey(key))
            shist.Add(key, 1);
        else
            shist[key]++;
    }

    public void Add(char key)
    {

        if (!shist.ContainsKey(key.ToString()))
            shist.Add(key.ToString(), 1);
        else
            shist[key.ToString()]++;
    }

    public void Add(int key)
    {
        if (!ihist.ContainsKey(key))
            ihist.Add(key, 1);
        else
            ihist[key]++;
    }

    private int valuetobin(double key)
    {
        int bin = 0;
        if (key > binmin)
        {
            if (key > binmax)
                bin = nbins + 1;
            else
            {
                bin = (int)((key - binmin) / binwid) + 1;
            }
        }
        return bin;
    }

    private double bintomin(int bin)
    {
        if (bin == 0)
            return binmin;
        if (bin > nbins)
            return binmax;
        return binmin + (bin - 1) * binwid;
    }

    private double bintomax(int bin)
    {
        if (bin == 0)
            return binmin;
        if (bin > nbins)
            return binmax;
        return binmin + bin * binwid;
    }

    public void Add(double key)
    {
        int bin = valuetobin(key);
        if (!ihist.ContainsKey(bin))
            ihist.Add(bin, 1);
        else
            ihist[bin]++;
    }

    public void SetBins(double min, double max, int nb)
    {
        if (nbins > MAXBINS - 2)
        {
            Console.WriteLine("Too many bins. Max " + (MAXBINS - 2).ToString());
            return;
        }
        else
        {
            binmax = max;
            binmin = min;
            nbins = nb;
            binwid = (max - min) / nbins;
            binlimits[0] = binmin;
            for (int i = 1; i <= nbins; i++)
            {
                binlimits[i] = binmin + i * binwid;
            }

            for (int i = 0; i <= nbins + 1; i++)
                if (!ihist.ContainsKey(i))
                    ihist.Add(i, 0);
        }
    }

    public void PrintIHist()
    {
        int total = 0;
        string s = "";
        foreach (int key in ihist.Keys)
        {
            Console.WriteLine(key + ": " + ihist[key].ToString());
            //s += key + ": " + ihist[key].ToString() + "\n";
            total += ihist[key];
        }
        Console.WriteLine("----Total : " + total.ToString());
        //s += "----Total : " + total.ToString() + "\n";
        //return s;
    }

    public string GetIHist()
    {
        int total = 0;
        string s = "";
        foreach (int key in ihist.Keys)
        {
            //Console.WriteLine(key + ": " + ihist[key].ToString());
            s += key + ": " + ihist[key].ToString() + "\n";
            total += ihist[key];
        }
        //Console.WriteLine("----Total : " + total.ToString());
        s += "----Total : " + total.ToString() + "\n";
        return s;
    }

    public void PrintDHist()
    {
        int total = 0;
        foreach (int key in ihist.Keys)
        {
            Console.WriteLine(bintomin(key).ToString() + " -- " + bintomax(key).ToString() + ": " + ihist[key].ToString());
            total += ihist[key];
        }
        Console.WriteLine("----Total : " + total.ToString());
    }

    public void PrintSHist()
    {
        int total = 0;
        foreach (string key in shist.Keys)
        {
            Console.WriteLine(key + ": " + shist[key].ToString());
            total += shist[key];
        }
        Console.WriteLine("----Total : " + total.ToString());
    }

    public string GetSHist()
    {
        int total = 0;
        string s = "";
        foreach (string key in shist.Keys)
        {
            s += key + ": " + shist[key].ToString()+"\n";
            total += shist[key];
        }
        s += "----Total : " + total.ToString() + "\n";
        return s;
    }
}
