// Các using không đổi
using DoAn.Models;
using DoAn.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DoAn.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<AddUserModel> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly DataContext _dataContext;

        public UserController(UserManager<AddUserModel> userManager, RoleManager<IdentityRole> roleManager, DataContext dataContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dataContext = dataContext;
        }

        // Phương thức Index để hiển thị danh sách người dùng
        [HttpGet]

        public async Task<IActionResult> Index()
        {
            var usersWithRoles = await (from u in _dataContext.Users
                                        join ur in _dataContext.UserRoles on u.Id equals ur.UserId
                                        join r in _dataContext.Roles on ur.RoleId equals r.Id
                                        select new
                                        {
                                            UserName = u.UserName,
                                            Email = u.Email,
                                            Id = u.Id,
                                            RoleNames = r.Name
                                        })
                            .GroupBy(u => u.Id)
                            .Select(group => new
                            {
                                UserName = group.FirstOrDefault().UserName,
                                Email = group.FirstOrDefault().Email,
                                Id = group.Key,
                                RoleNames = string.Join(", ", group.Select(g => g.RoleNames))
                            })
                            .ToListAsync();

            return View(usersWithRoles);
        }

        // Phương thức tạo người dùng mới (GET)
        [HttpGet]

        public async Task<IActionResult> Create()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");
            return View(new AddUserModel());
        }

        // Xử lý luồng tạo người dùng với xử lý lỗi tốt hơn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddUserModel user, string[] selectedRoles)
        {
            if (!ModelState.IsValid)
            {
                // Hiển thị lại danh sách vai trò nếu có lỗi
                var roles = await _roleManager.Roles.ToListAsync();
                ViewBag.Roles = new SelectList(roles, "Id", "Name");
                return View(user);
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu không thể để trống.");
                var roles = await _roleManager.Roles.ToListAsync();
                ViewBag.Roles = new SelectList(roles, "Id", "Name");
                return View(user);
            }

            var createUserResult = await _userManager.CreateAsync(user, user.PasswordHash);
            if (createUserResult.Succeeded)
            {
                var createUser = await _userManager.FindByEmailAsync(user.Email);

                if (selectedRoles != null && selectedRoles.Length > 0)
                {
                    var addToRoleResult = await _userManager.AddToRolesAsync(createUser, selectedRoles);
                    if (!addToRoleResult.Succeeded)
                    {
                        foreach (var error in addToRoleResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        var roles = await _roleManager.Roles.ToListAsync();
                        ViewBag.Roles = new SelectList(roles, "Id", "Name");
                        return View(user);
                    }
                }

                TempData["SuccessMessage"] = "Người dùng đã được tạo thành công!";
                return RedirectToAction("Index");
            }

            foreach (var error in createUserResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            var rolesList = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(rolesList, "Id", "Name");
            return View(user);
        }


        // Phương thức chỉnh sửa người dùng (GET)
        [HttpGet]
        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");

            var model = new AddUserModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Occupation = user.Occupation,
                RoleIds = (await _userManager.GetRolesAsync(user)).ToArray()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AddUserModel model, string[] selectedRoles)
        {
            if (model == null || string.IsNullOrEmpty(model.Id))
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                var roles = await _roleManager.Roles.ToListAsync();
                ViewBag.Roles = new SelectList(roles, "Id", "Name");
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            user.UserName = model.UserName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.Occupation = model.Occupation;

            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!removeResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Không thể loại bỏ vai trò hiện tại.");
                return View(model);
            }

            if (selectedRoles != null && selectedRoles.Length > 0)
            {
                var addToRoleResult = await _userManager.AddToRolesAsync(user, selectedRoles);
                if (!addToRoleResult.Succeeded)
                {
                    foreach (var error in addToRoleResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Cập nhật người dùng thành công!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            var rolesList = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(rolesList, "Id", "Name");
            return View(model);
        }

        // Xóa người dùng (GET)
        [HttpGet]
        [Route("Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // Xác nhận xóa người dùng (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Delete/{id}")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("Index");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View("Delete", user);
            }
        }
    }
}
