using System;
using System.Windows.Forms;

namespace Arquimedes
{
    public partial class FormSplash : Form
    {
        public FormSplash()
        {
            InitializeComponent();
        }

        private void FormSplash_Load(object sender, EventArgs e)
        {
            lblTitulo.Text = "Instituto Educativo Arquimedes";
            lblCargando.Text = "Cargando...";

            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;

            timerSplash.Interval = 40; // velocidad de la barra (ms por paso)
            timerSplash.Start();
        }

        private void timerSplash_Tick(object sender, EventArgs e)
        {
            if (progressBar1.Value < progressBar1.Maximum)
            {
                progressBar1.Value += 2;
                lblCargando.Text = progressBar1.Value + "%";
            }
            else
            {
                timerSplash.Stop();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void AbrirMenuPrincipal()
        {
            Form1 formPrincipal = new Form1();
            formPrincipal.Show();   // <-- esta línea es la que está duplicando la apertura
            this.Close();
        }
    }
}