﻿using AspNetIdentity2Permission.Mvc.Models;
using AutoMapper;
using Infragistics.Web.Mvc;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace AspNetIdentity2Permission.Mvc.Controllers
{
    public class RolePermissionsController : BaseController
    {
        // GET: RolePermissions
        public ActionResult Index(string roleId)
        {
            //取role列表
            var roles = _roleManager.Roles.ToList();
            //roleId是否为空
            if (string.IsNullOrWhiteSpace(roleId))
            {
                //取第一个role的id
                roleId = roles.FirstOrDefault().Id;
            }
            //放入viewbag，设置默认值
            ViewBag.RoleID = new SelectList(roles, "ID", "Description", roleId);
            //取角色权限列表
            var permissions = _roleManager.GetRolePermissions(roleId);
            //创建ViewModel
            var permissionViews = new List<PermissionViewModel>();

            var map = Mapper.CreateMap<ApplicationPermission, PermissionViewModel>();
            permissions.Each(t =>
            {
                var view = Mapper.Map<PermissionViewModel>(t);
                view.RoleID = roleId;
                permissionViews.Add(view);
            });
            //排序
            permissionViews.Sort(new PermissionViewModelComparer());
            return View(permissionViews);
        }

        // GET: RolePermissions/Details/5
        public ActionResult Details(string roleId, string permissionId)
        {
            if (string.IsNullOrWhiteSpace(roleId) || string.IsNullOrWhiteSpace(permissionId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ApplicationPermission applicationPermission = _db.Permissions.Find(permissionId);
            if (applicationPermission == null)
            {
                return HttpNotFound();
            }
            var view = new PermissionViewModel
            {
                Id = applicationPermission.Id,
                Action = applicationPermission.Action,
                Controller = applicationPermission.Controller,
                Description = applicationPermission.Description,
                RoleID = roleId
            };

            return View(view);
        }

        // POST: RolePermissions/Create

        [GridDataSourceAction]
        public  ActionResult Create(string roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var roles = _roleManager.Roles.ToList();
            ViewBag.RoleID = new SelectList(roles, "ID", "Description", roleId);

            //取角色权限ID
            var rolePermissions = _roleManager.GetRolePermissions(roleId);
            //取全部权限与角色权限的差集
            var allPermission = _db.Permissions.ToList();
            var permissions = allPermission.Except(rolePermissions);
            //创建ViewModel
            var permissionViews = new List<PermissionViewModel>();

            var map = Mapper.CreateMap<ApplicationPermission, PermissionViewModel>();
            permissions.Each(t =>
            {
                var view = Mapper.Map<PermissionViewModel>(t);

                permissionViews.Add(view);
            });
            //排序
            permissionViews.Sort(new PermissionViewModelComparer());
            return View(permissionViews.AsQueryable());
        }

        // POST: RolePermissions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(string roleId, IEnumerable<PermissionViewModel> data)
        {
            if (string.IsNullOrWhiteSpace(roleId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            //添加Permission
            foreach (var item in data)
            {
                var permission = new ApplicationRolePermission
                {
                    RoleId = roleId,
                    PermissionId = item.Id
                };
                //方法1,用set<>().Add()
                _db.Set<ApplicationRolePermission>().Add(permission);
            }
            //保存;
            var records = await _db.SaveChangesAsync();

            //return RedirectToAction("Index", new { roleId = roleId });
            //返回消息
            JsonResult result = new JsonResult();
            Dictionary<string, bool> response = new Dictionary<string, bool>();
            response.Add("Success", true);
            result.Data = response;
            return result;
        }

        // GET: RolePermissions/Delete/5
        public ActionResult Delete(string roleId, string permissionId)
        {
            if (string.IsNullOrWhiteSpace(roleId) || string.IsNullOrWhiteSpace(permissionId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ApplicationPermission applicationPermission = _db.Permissions.Find(permissionId);
            if (applicationPermission == null)
            {
                return HttpNotFound();
            }
            var view = new PermissionViewModel
            {
                Id = applicationPermission.Id,
                Action = applicationPermission.Action,
                Controller = applicationPermission.Controller,
                Description = applicationPermission.Description,
                RoleID = roleId
            };

            return View(view);
        }

        // POST: RolePermissions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string roleId, string permissionId)
        {
            if (string.IsNullOrWhiteSpace(roleId) || string.IsNullOrWhiteSpace(permissionId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (ModelState.IsValid)
            {
                //验证role与permission
                var role = await _roleManager.FindByIdAsync(roleId);
                var permission = _db.Permissions.Find(permissionId);
                if (role == null || permission == null)
                {
                    return HttpNotFound();
                }
                //删除Permission
                var entity = new ApplicationRolePermission { RoleId = roleId, PermissionId = permissionId };
                _db.Set<ApplicationRolePermission>().Attach(entity);
                _db.Entry(entity).State = EntityState.Deleted;

                var result = await _db.SaveChangesAsync();
            }
            return RedirectToAction("Index", new { roleId = roleId });
        }
    }
}