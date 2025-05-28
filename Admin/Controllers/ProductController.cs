using DoAn.Models;
using DoAn.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DoAn.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,ManageProduct")]
    public class ProductController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(DataContext context, IWebHostEnvironment webHostEnvironment)
        {
            _dataContext = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Product
        public async Task<IActionResult> Index()
        {
            var products = await _dataContext.Products
                .OrderByDescending(p => p.Id)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .ToListAsync();
            return View(products);
        }

        // GET: Admin/Product/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_dataContext.Category, "Id", "Name");
            ViewBag.Brands = new SelectList(_dataContext.Brands, "Id", "Name");
            return View();
        }

        // POST: Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductModel product)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(_dataContext.Category, "Id", "Name", product.CategoryId);
                ViewBag.Brands = new SelectList(_dataContext.Brands, "Id", "Name", product.BrandId);
                return View(product);
            }

            // Kiểm tra tính duy nhất của Slug
            var existingProduct = await _dataContext.Products
                .FirstOrDefaultAsync(p => p.Slug == product.Slug);
            if (existingProduct != null)
            {
                ModelState.AddModelError("", "Sản phẩm đã tồn tại với Slug này.");
                ViewBag.Categories = new SelectList(_dataContext.Category, "Id", "Name", product.CategoryId);
                ViewBag.Brands = new SelectList(_dataContext.Brands, "Id", "Name", product.BrandId);
                return View(product);
            }

            // Xử lý tải lên hình ảnh
            if (product.ImageUpload != null)
            {
                string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");
                string imageName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(product.ImageUpload.FileName);
                string filePath = Path.Combine(uploadsDir, imageName);

                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    await product.ImageUpload.CopyToAsync(fs);
                }
                product.Image = imageName;
            }
            else
            {
                product.Image = "default.jpg"; // Đặt hình ảnh mặc định nếu không tải lên
            }

            // Thêm sản phẩm vào cơ sở dữ liệu
            _dataContext.Add(product);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Thêm sản phẩm thành công!";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return RedirectToAction("Index");
            }

            var product = await _dataContext.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Slug == slug);

            if (product == null)
            {
                return NotFound();
            }

            // Lấy danh sách reviews 
            // Lấy danh sách reviews và filter theo ProductId
            ViewBag.Reviews = await _dataContext.Reviews
            .Where(r => r.ProductId == product.Id) // Sử dụng ProductId
                .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

            return View(product);
        }
        // GET: Admin/Product/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _dataContext.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(_dataContext.Category, "Id", "Name", product.CategoryId);
            ViewBag.Brands = new SelectList(_dataContext.Brands, "Id", "Name", product.BrandId);
            return View(product);
        }

        // POST: Admin/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductModel product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            // Dùng Include để load Reviews liên quan
            var existingProduct = await _dataContext.Products
                .Include(p => p.Reviews) // Thêm dòng này để load Reviews
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == product.Id);

            if (existingProduct == null)
            {
                return NotFound();
            }

            // Xử lý tải lên hình ảnh mới (giữ nguyên code của bạn)
            if (product.ImageUpload != null)
            {
                // ... (code xử lý image upload)
            }
            else
            {
                product.Image = existingProduct.Image;
            }

            try
            {
                _dataContext.Update(product); // Cập nhật product
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }
        // GET: Admin/Product/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _dataContext.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Xóa hình ảnh nếu không phải là hình mặc định
            if (!string.Equals(product.Image, "default.jpg", StringComparison.OrdinalIgnoreCase))
            {
                string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");
                string imagePath = Path.Combine(uploadsDir, product.Image);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _dataContext.Products.Remove(product);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Xóa sản phẩm thành công!";
            return RedirectToAction("Index");
        }

        // Kiểm tra sự tồn tại của sản phẩm
        private bool ProductExists(int id)
        {
            return _dataContext.Products.Any(e => e.Id == id);
        }
        [HttpPost]
        public async Task<IActionResult> SubmitReview(string productSlug, string Content, int Rating)
        {
            if (string.IsNullOrEmpty(productSlug) || string.IsNullOrEmpty(Content) || Rating < 1 || Rating > 5)
            {
                return BadRequest("Thông tin đánh giá không hợp lệ.");
            }

            // Tìm sản phẩm dựa trên slug
            var product = await _dataContext.Products.FirstOrDefaultAsync(p => p.Slug == productSlug);
            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm.");
            }

            var review = new ReviewModel
            {
                UserName = product.Id.ToString(),
                Content = Content,
                Rating = Rating,
                CreatedAt = DateTime.Now
            };

            _dataContext.Reviews.Add(review);
            await _dataContext.SaveChangesAsync();

            return RedirectToAction("Details", new { slug = productSlug });
        }
    }
}
