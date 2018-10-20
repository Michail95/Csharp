using System;
using System.Windows.Forms;

namespace FoldersConstructor
{
    // Класс диалогового окна ввода названия каталога
    public partial class EnterFolderNameForm : Form
    {
        public EnterFolderNameForm()
        {
            InitializeComponent();
        }

        // Возвращает и устанавливает имя каталога
        public String FolderName
        {
            get
            {
                return textBoxFolderName.Text;
            }

            set
            {
                textBoxFolderName.Text = value;
            }
        }
    }
}
