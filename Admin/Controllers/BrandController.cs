using DoAn.Models;
using DoAn.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoAn.Areas.Admin.Controllers
{
    [Area("Admin")]
    ///xác thực thì ms đc vào
	[Authorize(Roles ="Admin,Manage prodcut")]

	public class BrandController : Controller
    {
        private readonly DataContext _dataContext;

        public BrandController(DataContext context)
        {
            _dataContext = context;
        }
        [Route("Index")]

        public async Task<IActionResult> Index()
        {
            return View(await _dataContext.Brands
                .OrderByDescending(c => c.Id)
                .ToListAsync());
        } 



        // Phương thức hiển thị form tạo thương hiệu (GET)

        public IActionResult Create()
        {
            return View(); // Hiển thị form tạo thương hiệu
        }

        // Phương thức xử lý dữ liệu khi người dùng submit form (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BrandModel brand)
        {
            // Kiểm tra ModelState có hợp lệ không
            if (ModelState.IsValid)
            {
                // Tạo slug từ tên của thương hiệu (chuyển thành chữ thường và thay dấu cách thành dấu "-")
                brand.Slug = brand.Name.Replace(" ", "-").ToLower();

                // Kiểm tra xem thương hiệu với slug này đã tồn tại trong cơ sở dữ liệu chưa
                var existingBrand = await _dataContext.Brands.FirstOrDefaultAsync(c => c.Slug == brand.Slug);

                if (existingBrand != null)
                {
                    // Nếu đã tồn tại, trả về thông báo lỗi
                    ModelState.AddModelError("", "Thương hiệu đã tồn tại.");
                    return View(brand); // Trả lại form với lỗi
                }

                // Nếu thương hiệu chưa tồn tại, thêm thương hiệu mới vào cơ sở dữ liệu
                _dataContext.Add(brand);
                await _dataContext.SaveChangesAsync(); // Lưu thay đổi vào cơ sở dữ liệu

                // Thông báo thành công và chuyển hướng về trang danh sách thương hiệu
                TempData["success"] = "Thêm thương hiệu thành công.";
                return RedirectToAction("Index");
            }

            // Nếu ModelState không hợp lệ, trả về form với lỗi
            TempData["error"] = "Có một vài lỗi cần khắc phục.";
            return View(brand);
        }





        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _dataContext.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BrandModel brand)
        {
            if (ModelState.IsValid)
            {
                brand.Slug = brand.Name.Replace(" ", "-").ToLower();

                var existingBrand = await _dataContext.Brands.FirstOrDefaultAsync(c => c.Slug == brand.Slug && c.Id != brand.Id);
                if (existingBrand != null)
                {
                    ModelState.AddModelError("", "Thương hiệu đã tồn tại.");
                    return View(brand);
                }

                _dataContext.Update(brand);
                await _dataContext.SaveChangesAsync();

                TempData["success"] = "Cập nhật thương hiệu thành công.";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "Có một vài lỗi cần khắc phục.";
                return View(brand);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _dataContext.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }

            _dataContext.Brands.Remove(brand);
            await _dataContext.SaveChangesAsync();

            TempData["success"] = "Xóa thương hiệu thành công.";
            return RedirectToAction("Index");
        }
    }
}
