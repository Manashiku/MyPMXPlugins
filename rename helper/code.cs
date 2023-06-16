// pmxe already has all the using items written in another tab so i don't have to write it

// pmxe script plugin class
// this part cant be named anything else

public class CSScriptClass : PEPluginClass
{
    public CSScriptClass() : base()
    {
        m_option =  new PEPluginOption(false, true, "Rename bones from list");
    }

    public override void Run(IPERunArgs args)
    {
        base.Run(args);
        try
        {
            main_window f = new main_window(args);
            f.Show();            
        }
        catch(Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }
}

class main_window : Form 
{
    // connect
    public IPEPluginHost       host;
    public IPEBuilder          builder; 
    public IPEConnector        connect;
    public IPEPMDViewConnector view; 

    // pmx event handler
    public IPXPmx		   pmx;	
	public IList<IPXBone>  bone;	

    // public variables
    public bool issue_check; // if true, thees an issue and the issue code will execute 
    public List<string>  rename_list = new List<string>();
    public int[] problem_lines; // this will update with the lines to highlight
    // --------------------------------------------
    // actual window 

    public main_window(IPERunArgs args)
    {
        host = args.Host;
        builder = host.Builder; 
        connect = host.Connector;
        view =  host.Connector.View.PMDView;

        Size = new Size(395,450);
        Text = "Rename bones from list";
        FormBorderStyle = FormBorderStyle.FixedDialog; // will not be resizeable
        MaximizeBox = false;
        MinimizeBox = false;

        // List<string> rename_list = new List<string>();

        // ----------------------------------------
        Button pmx_bone_button = new Button()
        {
            Text = "Bone List From PMX", 
            Size = new Size(75,50), 
            Location = new Point(20, 49),
        };

        // current_pmx_bone_button.Click += new EventHandler(pmx_bone_button);
        Controls.Add(pmx_bone_button);

        Button clear_button = new Button()
        {
            Text = "Clear List", 
            Size = new Size(75,50), 
            Location = new Point(20, 49),
        };
        Controls.Add(pmx_bone_button);

        Button load_list_button = new Button()
        {
            Text = "Load List", 
            Size = new Size(75,25), 
            Location = new Point(20, 145),
        };
        Controls.Add(load_list_button);        
        
        Button save_list_button = new Button()
        {
            Text = "Save List", 
            Size = new Size(75,25), 
            Location = new Point(20, 115),
        };
        Controls.Add(save_list_button);

        Button rename_button = new Button()
        {
            Text = "Rename Bones", 
            Size = new Size(75,50), 
            Location = new Point(20, 343),
        };
        Controls.Add(rename_button);

        Label header_label = new Label()
        {
            Text = "rename bones from list or save bone list to file", 
            Size = new Size(200, 15),
            Location = new Point(20, 20),
        };
        Controls.Add(header_label);


        // ----------------------------------------
        // rename listbox
        ListBox rename_list_box = new ListBox();
        rename_list_box.Location = new Point(110,50);
        rename_list_box.Size = new Size(250, 350);


        // populate list with lists from buttons
        pmx_bone_button.Click += (sender, e) =>
        {
            rename_list_box.Items.Clear(); // clear anything that used to be in there
            rename_list.Clear(); // clear current rename list to make room for the pmx list
            get_current_info();
            for(int i = 0; i < bone.Count; ++i)
            {
                
                rename_list.Add(bone[i].Name);

                rename_list_box.Items.Add(i.ToString() + ": " + bone[i].Name);
            }
            update_with_current();
        };
        
        load_list_button.Click += (sender, e) =>
        {
            using (OpenFileDialog open_file = new OpenFileDialog())
            {
                open_file.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                if(open_file.ShowDialog() == DialogResult.OK)
                {
                    string[] list_of_names = File.ReadAllLines(open_file.FileName); // list of names as string array
                    rename_list = new List<string>(list_of_names); // use that string array as the new rename list
                    rename_list_box.Items.Clear(); // clear the list view
                    for(int n = 0; n < rename_list.Count; ++n)
                    {
                        rename_list_box.Items.Add(n.ToString()+": "+rename_list[n]); // populate list with names from opened file
                    }

                }
            }
        };

        save_list_button.Click += (sender, e) =>
        {
            using (SaveFileDialog save_file = new SaveFileDialog())
            {
                save_file.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                if(save_file.ShowDialog() == DialogResult.OK )
                {
                    string saved_list = "";
                    for(int s = 0; rename_list.Count > s; s++)
                    {
                        saved_list = saved_list + rename_list[s] + "\n";
                    }
                    File.WriteAllText(save_file.FileName, saved_list);
                }
            }
        };

        // its cool i can just have a global list to use throughout my form
        // (  :
        rename_button.Click += new EventHandler(rename_button_click);
        Controls.Add(rename_list_box);
    }
    // --------------------------------------------
    // button a
    void rename_button_click(object sender, EventArgs e)
    {
        get_current_info();

        if(rename_list.Count != 0)
        {
            int renamed = 0;
            
            // to keep track of what lines have issues
            int issues = 0;
            string issue_lines= "";
            
            for(int n = 0; n < rename_list.Count; n++)
            {
                int sep = rename_list[n].IndexOf("=");
                if(sep < 0)
                {
                    issue_lines = issue_lines + n.ToString() +", ";
                    issues++;
                    continue;
                }

                string old_name = rename_list[n].Substring(0, sep); // first half of line
                string new_name = rename_list[n].Substring(sep + 1);

                for(int b = 0; b < bone.Count;  b++)
                {
                    if(bone[b].Name == old_name)
                    {
                        bone[b].Name = new_name;
                        renamed++;
                    }
                }
            }
            if(issues > 0 )
            {
                MessageBox.Show("there were issues with these lines\n" + issue_lines );
            }
            
            MessageBox.Show("done\n" + renamed.ToString() +" bones renamed");
        }
        else
        {
            MessageBox.Show("there are no bones in the list\nplease load a list");
            return;
        }
        update_with_current();
    }

    public void get_current_info()
    {
        pmx = connect.Pmx.GetCurrentState(); // get current state
        bone = pmx.Bone; // get current list of bones
    }

    public void update_with_current()
    {
        connect.Pmx.Update(pmx); // update the info in pmx
        connect.Form.UpdateList(UpdateObject.All); // update form
        connect.View.PMDView.UpdateModel(); // refresh model ot reflect new changes
        connect.View.PMDView.UpdateView(); // refresh view to reflect new changes
    }

}