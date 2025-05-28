using DoAn.Models;
using DoAn.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAn.Areas.Admin.Controllers
{
    [Area("Admin")]
	[Authorize]
    [Authorize(Roles = "Admin")]

    public class CategoryController : Controller
    {
        private readonly DataContext _dataContext;

        public CategoryController(DataContext context)
        {
            _dataContext = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _dataContext.Category
                .OrderByDescending(c => c.Id)
                .ToListAsync();
            return View(categories);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryModel category)
        {
            if (ModelState.IsValid)
            {
                category.Slug = category.Name.Replace(" ", "-").ToLower();
                var existingCategory = await _dataContext.Category.FirstOrDefaultAsync(c => c.Slug == category.Slug);

                if (existingCategory != null)
                {
                    ModelState.AddModelError("", "Danh mục đã tồn tại.");
                    return View(category);
                }

                _dataContext.Add(category);
                await _dataContext.SaveChangesAsync();

                TempData["success"] = "Thêm danh mục thành công.";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "Có một vài lỗi cần khắc phục.";
                return View(category);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await _dataContext.Category.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryModel category)
        {
            if (ModelState.IsValid)
            {
                category.Slug = category.Name.Replace(" ", "-").ToLower();

                var existingCategory = await _dataContext.Category.FirstOrDefaultAsync(c => c.Slug == category.Slug && c.Id != category.Id);
                if (existingCategory != null)
                {
                    ModelState.AddModelError("", "Danh mục đã tồn tại.");
                    return View(category);
                }

                _dataContext.Update(category);
                await _dataContext.SaveChangesAsync();

                TempData["success"] = "Cập nhật danh mục thành công.";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "Có một vài lỗi cần khắc phục.";
                return View(category);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var category = await _dataContext.Category.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            _dataContext.Category.Remove(category);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Xóa danh mục thành công.";
            return RedirectToAction("Index");
        }
    }
}
