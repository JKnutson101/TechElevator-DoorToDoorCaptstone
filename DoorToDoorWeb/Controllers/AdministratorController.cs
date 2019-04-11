﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoorToDoorLibrary.DAL;
using DoorToDoorLibrary.Logic;
using DoorToDoorLibrary.Models;
using DoorToDoorWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DoorToDoorWeb.Controllers
{
    public class AdministratorController : AuthController
    {
        public AdministratorController(IDoorToDoorDAL db, IHttpContextAccessor httpContext) : base(db, httpContext)
        {

        }

        private AdminManagerListViewModel CreateAdminManagerListViewModel()
        {
            AdminManagerListViewModel managerList = new AdminManagerListViewModel();
            managerList.Managers = _db.GetAllManagers();
            managerList.Register = new RegisterViewModel();

            return managerList;
        }

        [HttpGet]
        public IActionResult Home()
        {
            ActionResult result = GetAuthenticatedView("Home", CreateAdminManagerListViewModel());

            if (Role.IsAdministrator)
            {
                return result;
            }
            else
            {
                return RedirectToAction("Login", "Home");
            }
        }

        [HttpPost]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            ActionResult result = null;

            if (Role.IsAdministrator)
            {
                try
                {

                    _db.MarkResetPassword(model.UserId);

                    TempData["resetSuccess"] = true;

                    result = RedirectToAction("Home", CreateAdminManagerListViewModel());
                }
                catch(Exception ex)
                {
                    ModelState.AddModelError($"resetFailed{model.UserId}", ex.Message);

                    result = RedirectToAction("Home", CreateAdminManagerListViewModel());
                }
            }
            else
            {
                result = RedirectToAction("Login", "Home");
            }

            return result;
        }

        [HttpPost]
        public ActionResult RegisterManager(AdminManagerListViewModel model)
        {
            ActionResult result = null;

            if (Role.IsAdministrator)
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        result = View("Home", CreateAdminManagerListViewModel());
                    }
                    else
                    {
                        User newUser = new User()
                        {
                            FirstName = model.Register.FirstName,
                            LastName = model.Register.LastName,
                            EmailAddress = model.Register.EmailAddress,
                            Password = model.Register.Password,
                            ConfirmPassword = model.Register.ConfirmPassword,
                            RoleId = (int)RoleManager.eRole.Manager
                        };

                        RegisterUser(newUser);

                        result = RedirectToAction("Home", CreateAdminManagerListViewModel());
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("invalid", ex.Message);

                    result = View("Home", CreateAdminManagerListViewModel());
                }
            }
            else
            {
                result = RedirectToAction("Login", "Home");
            }

            return result;
        }
    }
}