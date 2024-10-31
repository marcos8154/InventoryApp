using Inventory2.Data;
using Inventory2.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;
using static System.Net.Mime.MediaTypeNames;

namespace Inventory2
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ImportProducts : ContentPage
    {
        public ImportProducts()
        {
            InitializeComponent();
        }

        private async void btnOpenF_Clicked(object sender, EventArgs e)
        {
            await PickAndShow(new PickOptions()
            {
                PickerTitle = "Select Products Worksheet (csv)",
            });
        }


        async Task<FileResult> PickAndShow(PickOptions options)
        {
            try
            {
                var result = await FilePicker.PickAsync(options);
                if (result != null)
                {
                    if (result.FileName.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
                    {
                        System.IO.Stream stream = await result.OpenReadAsync();
                        byte[] data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);

                        string csv = Encoding.UTF8.GetString(data);

                        LoadCsv(csv);
                        FillSettings();
                        lbRemained.Text = $"File selected: {result.FileName}";
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                // The user canceled or something went wrong
            }

            return null;
        }

        private void FillSettings()
        {
            using (SQLiteContext db = new SQLiteContext())
            {
                ImportSettings s = db.ImportSettings.Find(1);
                if (s == null) return;

                pkId.SelectedIndex = s.ProductID;
                pkSKU.SelectedIndex = s.SKU;
                pkEAN.SelectedIndex = s.EAN;
                pkAddress.SelectedIndex = s.Address;
                pkDescription.SelectedIndex = s.Description;
                pkWarehouse.SelectedIndex = s.Warehouse;
            }
        }
        private string[] lines = new string[0];
        private char separator;
        private void LoadCsv(string csv)
        {
            lines = csv.Split('\r');
            separator = (lines[0].Contains(";")) ? ';' : ',';
            string[] header = lines[0].Split(separator);

            pkId.ItemsSource = header;
            pkDescription.ItemsSource = header;
            pkSKU.ItemsSource = header;
            pkEAN.ItemsSource = header;
            pkWarehouse.ItemsSource = header;
            pkAddress.ItemsSource = header;
        }

        private void btnSave_Clicked(object sender, EventArgs e)
        {
            using (SQLiteContext db = new SQLiteContext())
            {
                SaveSettings(db);
            }
            ProcessFile();
        }

        // Novo ProcessFile()
        //Na hora de importar, irá resetar a tabela do sql ou não?
        private void ProcessFile()
        {
            progress.IsVisible = true;
            btnSave.IsEnabled = false;
            btnOpenF.IsEnabled = false;

            Task.Run(async () =>
            {
                Stopwatch sw = new Stopwatch();
                int updateInterval = 10;
                int slidingWindowSize = 100;
                Queue<long> slidingWindow = new Queue<long>();

                using (SQLiteContext db = new SQLiteContext())
                {
                    ImportSettings s = db.ImportSettings.Find(1);

                    // Carregar todos os produtos relevantes de uma vez (se possível)
                    Dictionary<string, Product> productCache = db.Product
                        .Where(p => lines.Select(line => line[s.ProductID].ToString()).Contains(p.ProductId))
                        .ToDictionary(p => p.ProductId);

                    List<Product> newProducts = new List<Product>();

                    try
                    {
                        for (int x = 1; x <= lines.Length; x++)
                        {
                            sw.Start();
                            try
                            {
                                string rawline = lines[x].Replace("\n", "");
                                string[] line = rawline.Split(separator);
                                string productId = line[s.ProductID];
                                string address = (s.Address >= 0 ? line[s.Address] : "");

                                Product prod = productCache
                                    .FirstOrDefault(p => p.Value.ProductId == productId && p.Value.Address == address).Value;

                                if (prod == null)
                                {
                                    prod = new Product();
                                    newProducts.Add(prod);  // Adicione novos produtos à lista para inserção posterior
                                }

                                // Atualiza os dados do produto
                                prod.ProductId = line[s.ProductID];
                                prod.Description = (s.Description >= 0 ? line[s.Description] : "");
                                prod.SKU = (s.SKU >= 0 ? line[s.SKU] : "");
                                prod.EAN = (s.EAN >= 0 ? line[s.EAN] : "");
                                prod.Warehouse = (s.Warehouse >= 0 ? line[s.Warehouse] : "");
                                prod.Address = (s.Address >= 0 ? line[s.Address] : "");

                                if (prod.Id == 0)
                                {
                                    db.Add(prod);  // Se o produto for novo, adiciona no DB
                                }
                                else
                                {
                                    db.Update(prod);  // Não está funcionando a atualização do produto
                                }

                                if (x % 100 == 0)  // Salva em lote a cada 100 linhas
                                {
                                    try
                                    {
                                        await db.SaveChangesAsync();
                                    }
                                    catch (Exception e)
                                    {
                                        throw e;
                                    }
                                }

                                // Atualiza o progresso na UI
                                this.Dispatcher.BeginInvokeOnMainThread(delegate
                                {
                                    progress.Progress = (double)((double)x / (double)lines.Length);
                                });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error at line {x}: {ex.Message}");
                            }

                            sw.Stop();

                            // Adiciona o tempo da iteração na janela deslizante
                            slidingWindow.Enqueue(sw.ElapsedTicks);
                            if (slidingWindow.Count > slidingWindowSize)
                                slidingWindow.Dequeue();  // Remove os tempos mais antigos

                            // Calcula a média dos tempos na janela deslizante
                            long avg = (long)slidingWindow.Average();
                            long remained = avg * (lines.Length - x);
                            TimeSpan timeSpan = TimeSpan.FromTicks(remained);

                            // Atualiza o tempo restante a cada intervalo de linhas
                            if (x % updateInterval == 0 || x == lines.Length)
                            {
                                this.Dispatcher.BeginInvokeOnMainThread(delegate
                                {
                                    lbRemained.Text = $"Time remaining: {timeSpan.ToString(@"hh\:mm\:ss")}";
                                });
                            }

                            sw.Reset();
                        }

                        // Salva qualquer alteração pendente e os novos produtos
                        await db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }

                    // Certifique-se de que a UI é atualizada após o processo terminar
                    this.Dispatcher.BeginInvokeOnMainThread(() =>
                    {
                        progress.IsVisible = false;
                        /*btnSave.IsEnabled = true; //Verificar se é necessário deixar esses botões enabled ou não
                        btnOpenF.IsEnabled = true;*/
                        lbRemained.Text = "File successfully imported!";
                    });
                }
            });
        }

        // Antigo ProcessFile()
        /*private void ProcessFile()
        {
            progress.IsVisible = true;
            btnSave.IsEnabled = false;
            btnOpenF.IsEnabled = false;
            Task.Run(() =>
            {
                Stopwatch sw = new Stopwatch();
                int updateInterval = 10;  // Atualiza a cada 10 linhas
                int slidingWindowSize = 100; // Janela deslizante de 100 iterações
                Queue<long> slidingWindow = new Queue<long>();

                using (SQLiteContext db = new SQLiteContext())
                {
                    ImportSettings s = db.ImportSettings.Find(1);
                    for (int x = 1; x <= lines.Length; x++)
                    {
                        sw.Start();
                        try
                        {
                            string rawline = lines[x].Replace("\n", "");
                            string[] line = rawline.Split(separator);
                            string productId = line[s.ProductID];
                            string address = (s.Address >= 0 ? line[s.Address] : "");
                            Product prod = db.Product.FirstOrDefault(p => p.ProductId == productId && p.Address == address);
                            if (prod == null) prod = new Product();
                            prod.ProductId = line[s.ProductID];
                            prod.Description = (s.Description >= 0 ? line[s.Description] : "");
                            prod.SKU = (s.SKU >= 0 ? line[s.SKU] : "");
                            prod.EAN = (s.EAN >= 0 ? line[s.EAN] : "");
                            prod.Warehouse = (s.Warehouse >= 0 ? line[s.Warehouse] : "");
                            prod.Address = (s.Address >= 0 ? line[s.Address] : "");
                            if (prod.Id == 0) db.Add(prod);
                            else db.Update(prod);
                            db.SaveChanges();

                            this.Dispatcher.BeginInvokeOnMainThread(delegate
                            {
                                progress.Progress = (double)((double)x / (double)lines.Length);
                            });
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                        sw.Stop();

                        // Adiciona o tempo da iteração na janela deslizante
                        slidingWindow.Enqueue(sw.ElapsedTicks);
                        if (slidingWindow.Count > slidingWindowSize)
                            slidingWindow.Dequeue();  // Remove os tempos mais antigos

                        // Calcula a média apenas dos tempos dentro da janela deslizante
                        long avg = (long)slidingWindow.Average();
                        long remained = avg * (lines.Length - x);
                        TimeSpan timeSpan = TimeSpan.FromTicks(remained);

                        // Atualiza o tempo restante a cada intervalo de linhas
                        if (x % updateInterval == 0 || x == lines.Length)
                        {
                            this.Dispatcher.BeginInvokeOnMainThread(delegate
                            {
                                lbRemained.Text = $"Time remaining: {timeSpan.ToString(@"hh\:mm\:ss")}";
                            });
                        }

                        sw.Reset();
                    }
                }

                this.Dispatcher.BeginInvokeOnMainThread(delegate
                {
                    progress.IsVisible = false;
                    btnSave.IsEnabled = true;
                    btnOpenF.IsEnabled = true;
                });
            });
        }*/

        private void SaveSettings(SQLiteContext db)
        {
            ImportSettings s = db.ImportSettings.Find(1);
            if (s == null) s = new ImportSettings();

            s.ProductID = pkId.SelectedIndex;
            s.SKU = pkSKU.SelectedIndex;
            s.EAN = pkEAN.SelectedIndex;
            s.Address = pkAddress.SelectedIndex;
            s.Description = pkDescription.SelectedIndex;
            s.Warehouse = pkWarehouse.SelectedIndex;

            if (s.Id == 0) db.ImportSettings.Add(s);
            else db.ImportSettings.Update(s);
            db.SaveChanges();
        }

        private void ContentPage_Appearing(object sender, EventArgs e)
        {

        }
    }
}