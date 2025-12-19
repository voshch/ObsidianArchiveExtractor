using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ObsidianArchiveExtractor
{
    public partial class Form1 : Form
    {
        static ObsidianArchiveFile oaf;
        static OAFEntry exportEntry;
        FolderBrowserDialog folderBrowserDialog1;
        bool keepSubdirectories = false;
        string autoExtractFile = null;
        string autoExtractDir = null;
        bool autoExtractPreserveSubdirs = false;

        public Form1()
        {
            
            InitializeComponent();
            folderBrowserDialog1 = new FolderBrowserDialog();
            TextBoxStreamWriter tw = new TextBoxStreamWriter(textBox1);
            Console.SetOut(tw);
            
            // Subscribe to form shown event to run auto-extraction AFTER form is visible
            this.Shown += Form1_Shown;
        }
        
        private void Form1_Shown(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(autoExtractFile))
            {
                keepSubdirectories = autoExtractPreserveSubdirs;
                checkBoxSubdirectories.Checked = keepSubdirectories;
                
                try
                {
                    oaf = new ObsidianArchiveFile(autoExtractFile);
                    listBox1.Items.Clear();
                    foreach (OAFEntry entry in oaf.fileList)
                    {
                        listBox1.Items.Add(entry);
                    }
                    
                    Console.WriteLine("Loaded archive: " + autoExtractFile);
                    
                    if (!string.IsNullOrEmpty(autoExtractDir))
                    {
                        // Select all items
                        for (int i = 0; i < listBox1.Items.Count; i++)
                        {
                            listBox1.SetSelected(i, true);
                        }
                        
                        Console.WriteLine("Extracting all files to: " + autoExtractDir);
                        ExtractMultipleFiles(autoExtractDir);
                        
                        Console.WriteLine("Extraction complete. Closing application.");
                        System.Threading.Thread.Sleep(1000);
                        Application.Exit();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    MessageBox.Show("Failed to load archive: " + ex.Message, "Error");
                }
            }
        }

        public void AutoExtractMode(string inputFile, string outputDir, bool preserveSubdirectories = true)
        {
            autoExtractFile = inputFile;
            autoExtractDir = outputDir;
            autoExtractPreserveSubdirs = preserveSubdirectories;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OAF_finder.ShowDialog();
        }

        private void OAF_finder_FileOk(object sender, CancelEventArgs e)
        {
            listBox1.Items.Clear();
            GC.Collect();
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";
            textBox5.Text = "";
            checkBox1.Checked = false;
            //try
           // {
                oaf = new ObsidianArchiveFile(OAF_finder.FileName);
                foreach (OAFEntry entry in oaf.fileList)
                {
                    listBox1.Items.Add(entry);
                }
                
           // }
          //  catch (Exception ee)
            //{
            //    MessageBox.Show(ee.Message, "A problem!");
            //}
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItems.Count == 0)
                return;

            // Calculate sum of uncompressed sizes for all selected items
            long totalUncompressedSize = 0;
            long totalCompressedSize = 0;
            bool anyCompressed = false;

            foreach (OAFEntry ent in listBox1.SelectedItems)
            {
                totalUncompressedSize += ent.uncompressedSize;
                if (ent.compressed)
                {
                    totalCompressedSize += ent.compressedSize;
                    anyCompressed = true;
                }
            }

            checkBox1.Checked = anyCompressed;
            textBox2.Text = totalUncompressedSize.ToString();
            textBox3.Text = anyCompressed ? totalCompressedSize.ToString() : "N/A";
            
            // Show data offset of first selected item
            if (listBox1.SelectedItems.Count > 0)
            {
                OAFEntry firstEnt = (OAFEntry)listBox1.SelectedItems[0];
                textBox4.Text = firstEnt.dataOffset.ToString();
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select at least one file to extract.", "No Selection");
                return;
            }

            if (listBox1.SelectedItems.Count == 1)
            {
                // Single file: use SaveFileDialog
                Object o = listBox1.SelectedItem;
                OAFEntry ent = (OAFEntry)o;
                exportEntry = ent;
                saveFileDialog1.FileName = Path.GetFileName(ent.name);
                saveFileDialog1.ShowDialog();
            }
            else
            {
                // Multiple files: use FolderBrowserDialog
                folderBrowserDialog1.Description = "Select folder to extract " + listBox1.SelectedItems.Count + " files";
                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    ExtractMultipleFiles(folderBrowserDialog1.SelectedPath);
                }
            }
        }

        private void ExtractMultipleFiles(string outputFolder)
        {
            int successCount = 0;
            int failCount = 0;

            foreach (OAFEntry ent in listBox1.SelectedItems)
            {
                try
                {
                    string outputPath = keepSubdirectories 
                        ? Path.Combine(outputFolder, ent.name)
                        : Path.Combine(outputFolder, Path.GetFileName(ent.name));
                    
                    // Create subdirectories if needed
                    string dirPath = Path.GetDirectoryName(outputPath);
                    if (!Directory.Exists(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                    }

                    oaf.br.BaseStream.Seek(ent.dataOffset, SeekOrigin.Begin);
                    int size = (ent.compressed) ? ent.compressedSize : ent.uncompressedSize;
                    byte[] payload = oaf.br.ReadBytes(size);

                    FileStream fs = new FileStream(outputPath, FileMode.Create);
                    fs.Write(payload, 0, payload.Length);
                    fs.Close();

                    Console.WriteLine("Extracted: " + ent.name + " to " + outputPath);
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to extract " + ent.name + ": " + ex.Message);
                    failCount++;
                }
            }

            MessageBox.Show("Extracted " + successCount + " file(s)." + (failCount > 0 ? "\nFailed: " + failCount : ""), "Extraction Complete");
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            OAFEntry ent = exportEntry;

            oaf.br.BaseStream.Seek(ent.dataOffset, SeekOrigin.Begin);

            int size = (ent.compressed) ? ent.compressedSize : ent.uncompressedSize;
            byte[] payload = oaf.br.ReadBytes(size);

            FileStream fs = new FileStream(saveFileDialog1.FileName, FileMode.Create);

            fs.Write(payload, 0, payload.Length);

            fs.Close();

            Console.WriteLine("The contained file " + ent.name + " has been written to " + saveFileDialog1.FileName+".");

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select at least one file to extract.", "No Selection");
                return;
            }

            folderBrowserDialog1.Description = "Select folder to extract " + listBox1.SelectedItems.Count + " file(s)";
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                ExtractMultipleFiles(folderBrowserDialog1.SelectedPath);
            }
        }

        private void checkBoxSubdirectories_CheckedChanged(object sender, EventArgs e)
        {
            keepSubdirectories = checkBoxSubdirectories.Checked;
        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                // Select all items
                for (int i = 0; i < listBox1.Items.Count; i++)
                {
                    listBox1.SetSelected(i, true);
                }
                e.Handled = true;
            }
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            if (oaf == null)
                return;

            listBox1.Items.Clear();
            foreach (OAFEntry o in oaf.fileList)
            {
                if (o.name.Contains(textBox5.Text))
                listBox1.Items.Add(o);
            }
        }
    }

    public class TextBoxStreamWriter : TextWriter
    {
        TextBox _output = null;

        public TextBoxStreamWriter(TextBox output)
        {
            _output = output;
        }

        public override void Write(char value)
        {
            _output.AppendText(value.ToString());
        }

        public override Encoding Encoding
        {
            get { throw new NotImplementedException(); }
        }

        
    }

    public class ObsidianArchiveFile
    {
        
        String filePath;
        public BinaryReader br;
        public List<OAFEntry> fileList = new List<OAFEntry>();

        private static String readNullTermString(BinaryReader br)
        {
            StringBuilder sb = new StringBuilder();
            char c = ' ';
            while (br.BaseStream.Position < br.BaseStream.Length && (c = (char)br.ReadByte()) != 0)
            {
                sb.Append(c);
            }
            return sb.ToString();
        }
        
        public ObsidianArchiveFile(String filePath)
        {
            Console.WriteLine("Attempting to open " + filePath + " for analysis.");

            this.filePath = filePath;
            br = new BinaryReader(new FileStream(filePath, FileMode.Open));

            String magic = new String(br.ReadChars(4));

            if (!magic.Equals("OAF!"))
                throw new InvalidDataException("This does not appear to be an Obsidian Archive File (*.OAF).");

            br.ReadBytes(8);

            Int64 fileListPosition = br.ReadInt64();
            Console.WriteLine("File list should be found at offset " + fileListPosition);

            Int32 fileCount = br.ReadInt32();
            Console.WriteLine("Archive claims to house " + fileCount + " files.");

            Int64 position = br.BaseStream.Position;
                      
            br.BaseStream.Seek(fileListPosition, SeekOrigin.Begin);

            while(br.BaseStream.CanRead && br.BaseStream.Position < br.BaseStream.Length)
            {
                String fileName = readNullTermString(br);
                fileList.Add(new OAFEntry(fileName));
            }

            Console.WriteLine("File list complete.");

            br.BaseStream.Seek(position, SeekOrigin.Begin);
            for (int i = 0; i < fileCount; i++)
            {
                fileList[i].magic = br.ReadInt32();
                fileList[i].dataOffset = br.ReadInt32();
                fileList[i].compressed = (br.ReadInt32() == 0x10) ? true : false;
                fileList[i].uncompressedSize = br.ReadInt32();
                fileList[i].compressedSize = br.ReadInt32();
            }

            Console.WriteLine("File records built.");
            

        }

        
    }

    public class OAFEntry
    {
        public String name;
        public Int64 dataOffset;
        public bool compressed;
        public Int32 uncompressedSize;
        public Int32 compressedSize;
        public Int32 magic;

        public OAFEntry(String nName)
        {
            name = nName;
        }


        public override string  ToString()
        {
 	        return name;
        }
        
    }

}
