﻿using System;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MxWeiXinPF.Common;

namespace MxWeiXinPF.Web.admin.manager
{
    public partial class wxcodemgr : Web.UI.ManagePage
    {
        protected int totalCount;
        protected int page;
        protected int pageSize;
        BLL.wx_userweixin bll = new BLL.wx_userweixin();
        protected string keywords = string.Empty;

        protected void Page_Load(object sender, EventArgs e)
        {
            this.keywords = MXRequest.GetQueryString("keywords");

            this.pageSize = GetPageSize(10); //每页数量
            if (!Page.IsPostBack)
            {
                //ChkAdminLevel("wcodemgr", MXEnums.ActionEnum.View.ToString()); //检查权限
                RptBind(CombSqlTxt(keywords), "uid asc, createDate desc");
            }
        }

        #region 数据绑定=================================
        private void RptBind(string _strWhere, string _orderby)
        {
            Model.manager model = GetAdminInfo(); //取得当前管理员信息
            _strWhere = "isDelete=0 " + _strWhere;
            this.page = MXRequest.GetQueryInt("page", 1);
            txtKeywords.Text = this.keywords;

            Model.manager adminEntity = GetAdminInfo(); //取得当前管理员信息
            _strWhere += " and (agentId=" + adminEntity.id + " or uId=" + adminEntity.id+" )";

            DataSet ds = bll.GetUserWeiXinList(this.pageSize, this.page, _strWhere, _orderby, out this.totalCount);

            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                DataRow dr;
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    dr = ds.Tables[0].Rows[i];
                    DateTime endDate = MyCommFun.Obj2DateTime(dr["endDate"]);
                    if (endDate < DateTime.Now)
                    {
                        dr["daoqistr"] = "<span class='guoqi'>" + endDate.ToString("yyyy-MM-dd") + "[已过期]</span>";
                    }
                    else if (endDate < DateTime.Now.AddDays(20))
                    {
                        dr["daoqistr"] = "<span class='kuaidaoqi'>" + endDate.ToString("yyyy-MM-dd") + "[需尽快充值]</span>";
                    }
                    else
                    {
                        dr["daoqistr"] = "<span class='weiguoqi'>" + endDate.ToString("yyyy-MM-dd") + "</span>";
                    }

                }
            }

            this.rptList.DataSource = ds;
            this.rptList.DataBind();

            //绑定页码
            txtPageNum.Text = this.pageSize.ToString();
            string pageUrl = Utils.CombUrlTxt("wxcodemgr.aspx", "keywords={0}&page={1}", this.keywords, "__id__");
            PageContent.InnerHtml = Utils.OutPageList(this.pageSize, this.page, this.totalCount, pageUrl, 8);
        }
        #endregion

        #region 组合SQL查询语句==========================
        protected string CombSqlTxt(string _keywords)
        {
            StringBuilder strTemp = new StringBuilder();
            _keywords = _keywords.Replace("'", "");
            if (!string.IsNullOrEmpty(_keywords))
            {
                strTemp.Append(" and ( wxName like  '%" + _keywords + "%' or   weixinCode like '%" + _keywords + "%' or  user_name like '%" + _keywords + "%')");
            }

            return strTemp.ToString();
        }
        #endregion

        #region 返回每页数量=============================
        private int GetPageSize(int _default_size)
        {
            int _pagesize;
            if (int.TryParse(Utils.GetCookie("wxcodemgr_page_size"), out _pagesize))
            {
                if (_pagesize > 0)
                {
                    return _pagesize;
                }
            }
            return _default_size;
        }
        #endregion

        //关健字查询
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            Response.Redirect(Utils.CombUrlTxt("wxcodemgr.aspx", "keywords={0}", txtKeywords.Text));
        }

        //设置分页数量
        protected void txtPageNum_TextChanged(object sender, EventArgs e)
        {
            int _pagesize;
            if (int.TryParse(txtPageNum.Text.Trim(), out _pagesize))
            {
                if (_pagesize > 0)
                {
                    Utils.WriteCookie("wxcodemgr_page_size", _pagesize.ToString(), 14400);
                }
            }
            Response.Redirect(Utils.CombUrlTxt("wxcodemgr.aspx", "keywords={0}", this.keywords));
        }

        //批量删除
        protected void btnDelete_Click(object sender, EventArgs e)
        {
         
            int sucCount = 0;
            int errorCount = 0;
            BLL.wx_userweixin bll = new BLL.wx_userweixin();
            for (int i = 0; i < rptList.Items.Count; i++)
            {
                int id = Convert.ToInt32(((HiddenField)rptList.Items[i].FindControl("hidId")).Value);
                CheckBox cb = (CheckBox)rptList.Items[i].FindControl("chkId");
                if (cb.Checked)
                {
                    if (bll.DeleteWeixin(id))
                    {
                        sucCount += 1;
                    }
                    else
                    {
                        errorCount += 1;
                    }
                }
            }

            //bool isAgent = false;
            //BLL.wx_agent_info aBll = new BLL.wx_agent_info();
            //Model.manager adminEntity = GetAdminInfo(); //取得管理员信息
            //Model.wx_agent_info agent = new Model.wx_agent_info();
            //if (adminEntity.agentLevel > 0)
            //{
            //    isAgent = true;
            //    agent = aBll.GetAgentModel(adminEntity.id);
            //}
            //if (isAgent && agent != null)
            //{
            //    如果为代理商，则将起微帐号数量减掉
            //    agent.wcodeNum -= sucCount;
            //    aBll.Update(agent);
            //}


            AddAdminLog(MXEnums.ActionEnum.Delete.ToString(), "删除微信号信息" + sucCount + "条，失败" + errorCount + "条"); //记录日志

            JscriptMsg("删除成功" + sucCount + "条，失败" + errorCount + "条！", Utils.CombUrlTxt("wxcodemgr.aspx", "keywords={0}", this.keywords), "Success");
        }
    }
}