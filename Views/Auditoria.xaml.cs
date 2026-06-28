using Proyecto_taller.ViewModels;
using System.Windows.Controls;

namespace Proyecto_taller.Views
{
    public partial class Auditoria : Page
    {
        public Auditoria()
        {
            InitializeComponent();
            DataContext = new AuditoriaViewModel();
        }
    }
}
