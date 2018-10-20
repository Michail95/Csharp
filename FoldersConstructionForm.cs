using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FoldersConstructor
{
    public partial class FoldersConstructorForm : Form
    {
        [Serializable]
        protected class FolderNode
        {
            public String _folderName = String.Empty;
            public int _folderNumber = 0;
            public List<FolderNode> _subFolders = new List<FolderNode>();

            // Возвращает элемент подкаталога по его номеру
            public FolderNode this[int number]
            {
                get
                {
                    //  Ищем в списке подкаталогов каталог с заданным номером
                    foreach (FolderNode folder in _subFolders)
                    {
                        if (folder._folderNumber == number)
                            return folder;
                    }

                    return null;
                }
            }

            // Возвращает элемент подкаталога по его имени
            public FolderNode this[String name]
            {
                get
                {
                    foreach (FolderNode folder in _subFolders)
                    {
                        if (folder._folderName == name)
                            return folder;
                    }

                    return null;
                }
            }

            // Используется при десериализации объекта
            protected FolderNode()
            {
            }

            public FolderNode(String name, int number)
            {
                _folderName = name;
                _folderNumber = number;
            }

            public FolderNode(int number)
            {
                _folderNumber = number;
                _folderName = "FolderConstructor-Noname-" + number.ToString();
            }

            // Добавляет подкаталог в список подкаталогов данного каталога
            public void Add(FolderNode subFolder)
            {
                _subFolders.Add(subFolder);
            }

            // Удаляет подкаталог из списка подкаталогов данного каталога
            public FolderNode Remove(FolderNode removedFolder)
            {
                _subFolders.Remove(removedFolder);
                return removedFolder;
            }

            //Удаляет подкаталог с заданным номером из списка подкаталогов данного каталога
            public FolderNode Remove(int number)
            {
                //  Ищем в списке подкаталогов каталог с заданным номером
                foreach (FolderNode folder in _subFolders)
                {
                    //  Если нашли, то удаляем его из списка и выходим
                    if (folder._folderNumber == number)
                        return Remove(folder);
                }

                return null;
            }

            // Удаляет подкаталог с заданным именем из списка подкаталогов данного каталога
            public FolderNode Remove(String name)
            {
                foreach (FolderNode folder in _subFolders)
                {
                    if (folder._folderName == name)
                        return Remove(folder);
                }

                return null;
            }
        }

        // Поля класса FoldersConstructorForm

        private List<FolderNode> _folders = null;
        private TreeNode _parentNode = null;
        private String _filename = String.Empty;
        private int _unNamedNumber = 0;
        private bool _isModified = false;


        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FoldersConstructorForm());
        }

        public FoldersConstructorForm()
        {
            InitializeComponent();

            EnableAll(false);
        }

        // Создает новую структуру дерева каталогов.
        private void ToolStripMenuItem_File_New_Click(object sender, EventArgs e)
        {
            bool bResult = New();
        }

        // Восстанавливает структуру дерева каталога из файла (десериализация).
        private void ToolStripMenuItem_File_Open_Click(object sender, EventArgs e)
        {
            bool bReturn = Open();
        }

        // Перезаписывает структуру дерева каталогов в файле или создает новый файл дерева каталогов, если он не существует
        // (сериализация).
        private void ToolStripMenuItem_File_Save_Click(object sender, EventArgs e)
        {
            if (Save())
                _isModified = false;
        }

        // Сохраняет структуру дерева каталогов (сериализация) под новым именем.
        private void ToolStripMenuItem_File_SaveAs_Click(object sender, EventArgs e)
        {
            if (Save(true))
                _isModified = false;
        }

        // Выполняет загрузку дерева каталогов из указанной папки (или диска) компьютера.
        private void ToolStripMenuItem_File_Upload_Click(object sender, EventArgs e)
        {
            //  При необходимости производим сохранение
            if (_isModified)
            {
                if (!Save())
                    return;
            }

            _parentNode = null;
            _filename = String.Empty;
            _unNamedNumber = 0;
            _isModified = false;

            //  Удаляем все элементы дерева каталогов
            treeView_Folders.Nodes.Clear();

            EnableAll(false);

            FolderBrowserDialog foldersDialog = new FolderBrowserDialog();
            if (foldersDialog.ShowDialog() == DialogResult.OK)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(foldersDialog.SelectedPath);

                LoadDirectoryStructure(dirInfo, treeView_Folders.Nodes);
                treeView_Folders.ExpandAll();

                EnableAll(true);
            }
        }


        //Выполняет создание дерева каталогов в указанной папке (или в корне диска) компьютера.
        private void ToolStripMenuItem_File_Download_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog foldersDialog = new FolderBrowserDialog();
            if (foldersDialog.ShowDialog() == DialogResult.OK)
            {
                //  Если есть каталог FoldersConstruction, удалим его со всем содержимым
                DirectoryInfo dirInfo = new DirectoryInfo(foldersDialog.SelectedPath);
                if (dirInfo.Name == "FoldersConstruction")
                {
                    DirectoryInfo parent = dirInfo.Parent;
                    dirInfo.Delete(true);
                    dirInfo = parent;
                }

                //  Создадим каталог FoldersConstruction
                dirInfo = dirInfo.CreateSubdirectory("FoldersConstruction");

                CreateDirectoryStructure(dirInfo, treeView_Folders.Nodes);
            }
        }


        private void ToolStripMenuItem_File_Quit_Click(object sender, EventArgs e)
        {
            Close();
        }

        //Создает новое дерево каталогов.
        private void ToolStripMenuItem_Folder_NewTree_Click(object sender, EventArgs e)
        {
            TreeNode node = NewTreeNode("Укажите имя корневого каталога");
            if (node != null)
            {
                //  Добавляем созданный элемент в качестве корневого
                treeView_Folders.Nodes.Add(node);
                treeView_Folders.SelectedNode = node;
                _parentNode = node;
                _isModified = true;

                EnableAll(true);
            }
        }


        //Выбирает родительский каталог.
        private void ToolStripMenuItem_Folder_SelectParent_Click(object sender, EventArgs e)
        {
            _parentNode = treeView_Folders.SelectedNode;
        }

        //Создает дочерний каталог для установленного .
        private void ToolStripMenuItem_Folder_CreateChild_Click(object sender, EventArgs e)
        {
            //  Создаем корневой элемент каталога
            String str = (_parentNode == null) ? "Укажите имя корневого каталога" : "Укажите имя дочернего каталога";
            TreeNode node = NewTreeNode(str);
            if (node != null)
            {
                //  Добавляем созданный элемент
                if (_parentNode == null)
                {
                    //  Добавляем корневой каталог
                    treeView_Folders.Nodes.Add(node);
                }
                else
                {
                    //  Добавляем дочерний каталог
                    _parentNode.Nodes.Add(node);
                }

                //  Считаем созданный каталог родительским
                treeView_Folders.SelectedNode = node;
                _parentNode = node;
                _isModified = true;
            }
        }

        //Удаляет выбранный каталог и все его поддерево каталогов.
        private void toolStripMenuItem_Folder_Remove_Click(object sender, EventArgs e)
        {
            //  Для выбранного каталога
            TreeNode node = treeView_Folders.SelectedNode;
            TreeNode parentNode = node.Parent;
            if (parentNode == null)
            {
                //  Удаляем корневой каталог
                treeView_Folders.Nodes.Remove(node);

                //  Проверяем, есть ли еще корневые каталоги
                if (treeView_Folders.Nodes.Count == 0)
                {
                    //  Это был единственный корневой каталог
                    bool bResult = New();
                }
                else
                {
                    //  Выбираем в качестве родительского каталога первый из корневых
                    _parentNode = treeView_Folders.Nodes[0];
                    treeView_Folders.SelectedNode = _parentNode;
                }
            }
            else
            {
                parentNode.Nodes.Remove(node);

                //  Выбираем в качестве родительского каталог, которому принадлежал удаляемый каталог
                _parentNode = parentNode;
                treeView_Folders.SelectedNode = _parentNode;
            }

            _isModified = true;
        }

        //Выполняет создание нового дерева каталогов
        private bool New()
        {
            //  При необходимости производим сохранение
            if (_isModified)
            {
                if (!Save())
                    return false;
            }

            _parentNode = null;
            _filename = String.Empty;
            _unNamedNumber = 0;
            _isModified = false;

            //  Удаляем все элементы дерева каталогов
            treeView_Folders.Nodes.Clear();

            EnableAfterNew();

            return true;
        }


        private bool Open()
        {
            //  При необходимости производим сохранение
            if (_isModified)
            {
                if (!Save())
                    return false;
            }

            _parentNode = null;
            _filename = String.Empty;
            _unNamedNumber = 0;
            _isModified = false;

            //  Удаляем все элементы дерева каталогов
            treeView_Folders.Nodes.Clear();

            EnableAll(false);

            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.DefaultExt = "fcf";
            openDialog.Filter = "Файлы сериализации (*.fcf)|*.fcf|Все файлы (*.*)|*.*";
            openDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            openDialog.RestoreDirectory = true;
            if (openDialog.ShowDialog() != DialogResult.OK)
                return false;

            //  Выполняем десериализацию
            using (Stream fileStream = File.OpenRead(openDialog.FileName))
            {
                BinaryFormatter deserializer = new BinaryFormatter();

                _unNamedNumber = (int)deserializer.Deserialize(fileStream);
                _folders = (List<FolderNode>)deserializer.Deserialize(fileStream);
            }

            //  Создаем дерево каталогов TreeView
            PostDeserialization();
            _folders = null;

            EnableAll(true);

            return true;
        }

          /* bSaveRequist При указании true выдает запрос на сохранение дерева каталогов (сериализация) со сменой 
          имени файла. Значение по умолчанию - false, в этом случае файл перезаписывается, если из него производилась загрузка.
          Если создавалось новое дерево каталогов, то запрос имени файла произходит всегда. */
        private bool Save(bool bSaveRequist = false)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            String sFileName = _filename;

            //  При необходимости задаем путь для сохранения
            if (bSaveRequist || _filename == String.Empty)
            {
   
                saveDialog.CreatePrompt = true;
                saveDialog.OverwritePrompt = true;
                saveDialog.FileName = "Folders";
                saveDialog.DefaultExt = "fcf";
                saveDialog.Filter = "Файлы сериализации (*.fcf)|*.fcf|Все файлы (*.*)|*.*";
                saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                
                if (saveDialog.ShowDialog() != DialogResult.OK)
                    return false;

                //  Запоминаем имя файла
                sFileName = saveDialog.FileName;
            }

            
            PreSerialization();

           
            using (Stream fileStream = File.Create(sFileName))
            {
                BinaryFormatter serializer = new BinaryFormatter();

                serializer.Serialize(fileStream, _unNamedNumber);
                serializer.Serialize(fileStream, _folders);
            }

            _isModified = false;
            _folders = null;

            //  Сохраним имя файла
            _filename = sFileName;

            return true;
        }

        private void EnableAll(bool bEnable)
        {
            //  Блокируем/разблокируем пункты меню
            ToolStripMenuItem_File_Save.Enabled = bEnable;
            ToolStripMenuItem_File_SaveAs.Enabled = bEnable;
            ToolStripMenuItem_File_Download.Enabled = bEnable;
            ToolStripMenuItem_Folder_NewTree.Enabled = bEnable;
            ToolStripMenuItem_Folder_SelectParent.Enabled = bEnable;
            ToolStripMenuItem_Folder_CreateChild.Enabled = bEnable;
            toolStripMenuItem_Folder_Remove.Enabled = bEnable;

            //  Блокируем/разблокируем элемент панели инструментов
            toolStripButton_Save.Enabled = bEnable;
            toolStripButton_Download.Enabled = bEnable;
            toolStripButton_NewTree.Enabled = bEnable;
            toolStripButton_Parent.Enabled = bEnable;
            toolStripButton_Child.Enabled = bEnable;
            toolStripButton_Remove.Enabled = bEnable;
        }

        private void EnableAfterNew()
        {
            //  Блокируем/разблокируем пункты меню
            ToolStripMenuItem_File_Save.Enabled = false;
            ToolStripMenuItem_File_SaveAs.Enabled = false;
            ToolStripMenuItem_File_Download.Enabled = false;
            ToolStripMenuItem_Folder_NewTree.Enabled = true;
            ToolStripMenuItem_Folder_SelectParent.Enabled = false;
            ToolStripMenuItem_Folder_CreateChild.Enabled = false;
            toolStripMenuItem_Folder_Remove.Enabled = false;

            //  Блокируем/разблокируем элемент панели инструментов
            toolStripButton_Save.Enabled = false;
            toolStripButton_Download.Enabled = false;
            toolStripButton_NewTree.Enabled = true;
            toolStripButton_Parent.Enabled = false;
            toolStripButton_Child.Enabled = false;
            toolStripButton_Remove.Enabled = false;
        }

       /* Создает элемент дерева каталогов
        title Отображаемый заголовок окна если он необходим.
          Созданный элемент дерева каталогов или null.*/
        private TreeNode NewTreeNode(String title = null)
        {
            EnterFolderNameForm form = new EnterFolderNameForm();
            if (title != null && title != String.Empty && title != "")
                form.Text = title;
            String folderName = String.Format("Untitled Folder {0}", ++_unNamedNumber);
            form.FolderName = folderName;
            if (form.ShowDialog() == DialogResult.OK)
            {
                //  Создаем элемент дерева каталогов
                TreeNode node = new TreeNode(form.FolderName);
                node.Tag = _unNamedNumber;
                return node;
            }
            
            return null;
        }


        /*
         Метод void PreSerialization() класса FoldersConstructorForm
         Вызывается для подготовки данных для сериализации*/
        private void PreSerialization()
        {
            //  Создаем список каталогов
            _folders = new List<FolderNode>();

            PreSerialization(_folders, treeView_Folders.Nodes);
        }

        private void PreSerialization(List<FolderNode> folders, TreeNodeCollection treeNodes)
        {
            //  В цикле формируем список папок
            foreach (TreeNode node in treeNodes)
            {
                FolderNode folder = new FolderNode(node.Text, (int)node.Tag);
                folders.Add(folder);

                PreSerialization(folder._subFolders, node.Nodes);
            }
        }

        // Вызывается для подготовки восстановления дерева каталогов в элементе управления TreeView
        private void PostDeserialization()
        {
            //  Вызываем PostDeserialization для элементов каталога
            PostDeserialization(_folders, treeView_Folders.Nodes);
            treeView_Folders.ExpandAll();
        }

      /* Восстанавливает поддерево каталогов
        folders Список подкаталогов.
        treeNodes Узел поддерева.*/
        private void PostDeserialization(List<FolderNode> folders, TreeNodeCollection treeNodes)
        {
            //  В цикле формируем дерево каталогов
            foreach (FolderNode folder in folders)
            {
                TreeNode node = new TreeNode(folder._folderName);
                node.Tag = folder._folderNumber;
                treeNodes.Add(node);

                PostDeserialization(folder._subFolders, node.Nodes);
            }
        }

        private void LoadDirectoryStructure(DirectoryInfo dirIndo, TreeNodeCollection treeNodes)
        {
            //  Получаем список подкаталогов
            IEnumerable<DirectoryInfo> subDirsInfo = dirIndo.EnumerateDirectories();

            foreach (DirectoryInfo subDirInfo in subDirsInfo)
            {
                TreeNode node = new TreeNode(subDirInfo.Name);
                treeNodes.Add(node);

                LoadDirectoryStructure(subDirInfo, node.Nodes);
            }
        }

      /*   Создает структуру каталогов в указанном местоположении на диске компьютера
         dirIndo Элемент каталога на диске, в котором создаются подкаталоги. 
         treeNodes Узел поддерева. */
        private void CreateDirectoryStructure(DirectoryInfo dirInfo, TreeNodeCollection treeNodes)
        {
            //  Создаем подкаталоги указанного каталога
            foreach (TreeNode node in treeNodes)
            {
                DirectoryInfo subDirInfo = dirInfo.CreateSubdirectory(node.Text);

                CreateDirectoryStructure(subDirInfo, node.Nodes);
            }
        }
    }
}
