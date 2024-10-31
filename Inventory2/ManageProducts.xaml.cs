using Inventory2.Data;
using Inventory2.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Inventory2
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ManageProducts : ContentPage
    {
        private const int PageSize = 100; // Produtos por página
        private int currentPage = 0; // Página atual
        private bool isLoading = false; // Para evitar carregamentos simultâneos
        private bool allProductsLoaded = false; // Para marcar quando todos os produtos foram carregados
        private ObservableCollection<Product> productList = new ObservableCollection<Product>();

        public ManageProducts()
        {
            InitializeComponent();
            ProductListView.ItemsSource = productList;
            LoadProducts(); // Carrega a primeira página de produtos

            // Associa o evento ItemAppearing para detecção de rolagem
            ProductListView.ItemAppearing += OnItemAppearing;
        }
        private async void LoadProducts(string searchTerm = null)
        {
            if (isLoading || allProductsLoaded) return; // Evita carregamento duplicado ou desnecessário

            isLoading = true;

            try
            {
                // Carrega produtos com filtro de busca, se aplicável
                var products = await GetProductsFromDatabase(currentPage, PageSize, searchTerm);

                if (currentPage == 0 && string.IsNullOrEmpty(searchTerm))
                {
                    // Exibe a contagem total de produtos na inicialização (sem filtro de busca)
                    var totalProducts = await GetTotalProductsCount();
                    lbProducts.Text = $"Total products: {totalProducts}";
                }

                if (products.Count == 0)
                {
                    allProductsLoaded = true; // Marca quando todos os produtos foram carregados
                }
                else
                {
                    foreach (var product in products)
                    {
                        productList.Add(product); // Adiciona produtos à ObservableCollection
                    }
                    currentPage++; // Incrementa a página para a próxima requisição
                }
            }
            finally
            {
                isLoading = false; // Conclui o carregamento
            }
        }
        private async Task<int> GetTotalProductsCount()
        {
            using (var db = new SQLiteContext())
            {
                return await db.Product.CountAsync(); // Conta todos os produtos na tabela
            }
        }
        private async Task<int> GetFilteredProductsCount(string searchTerm)
        {
            using (var db = new SQLiteContext())
            {
                return await db.Product
                    .CountAsync(p => p.Description.ToLower().Contains(searchTerm.ToLower()));
            }
        }
        private async Task<List<Product>> GetProductsFromDatabase(int page, int pageSize, string searchTerm = null)
        {
            using (var db = new SQLiteContext())
            {
                var query = db.Product.AsNoTracking();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    // Converte o termo e os campos de pesquisa para minúsculas para uma busca case-insensitive
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(p => p.Description.ToLower().Contains(searchTerm) || p.Description.ToLower().Contains(searchTerm));
                }

                return await query
                    .OrderBy(p => p.Id)
                    .Skip(page * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
        }
        private async Task PreloadNextPage()
        {
            if (!allProductsLoaded && !isLoading)
            {
                await Task.Delay(200); // Simula um leve atraso antes do pré-carregamento
                LoadProducts();
            }
        }
        private void OnItemAppearing(object sender, ItemVisibilityEventArgs e)
        {
            var item = e.Item as Product;
            if (item == productList.LastOrDefault())
            {
                // Pré-carrega a próxima página quando o penúltimo item aparece
                Task.Run(() => PreloadNextPage());
            }
        }
        private async void btnSearch_Clicked(object sender, EventArgs e)
        {
            string searchTerm = entrySearch.Text?.Trim();

            // Redefine a lista e parâmetros de paginação para uma nova busca
            productList.Clear();
            currentPage = 0;
            allProductsLoaded = false;

            // Carrega produtos com o termo de busca
            LoadProducts(searchTerm);

            //Esse if está dando erro na hora de jogar os produtos, fazer uma task e metodo assíncrono pra pegar os produtos
            //

            if (string.IsNullOrEmpty(searchTerm))
            {
                // Quando não há termo de pesquisa, mostra o total de produtos
                var totalProducts = await GetTotalProductsCount();
                lbProducts.Text = $"Total products: {totalProducts}";
            }
            else
            {
                // Quando há um termo de pesquisa, mostra o total filtrado
                var filteredProductsCount = await GetFilteredProductsCount(searchTerm);
                lbProducts.Text = $"{filteredProductsCount} Products With '{searchTerm}'";
            }

            if (productList.Count > 0)
            {
                ProductListView.ScrollTo(productList[0], ScrollToPosition.Start, animated: false);
            }
        }
    }
}