﻿using EFRailWay.Concrete.KIS;
using EFRailWay.Entities;
using EFRailWay.Entities.KIS;
using EFRailWay.KIS;
using TransferWagons.Railcars;
//using EFRailWay.Settings;
using EFWagons.Entities;
//using Errors;
using Logs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransferWagons.Transfers
{
    public enum errorTransfer : int
    {
        global = -1,
        no_stations = -2,
        no_ways = -3,
        no_wagons = -4,
        no_owner_country = -5,
        no_owner = -6,
        no_shop = -7,
        no_gruz = -8,
        no_wagon_is_list = -9,
        no_wagon_is_nathist = -10,
        no_godn = -11,
        no_del_output =-12,
        no_tupik =-13,
        no_station_nazn = -14
    }
    /// <summary>
    /// Класс данных результат переноса массва данных
    /// </summary>
    public class ResultTransfers 
    {
        public int counts { get; set; }
        public int result { get; set; }
        public int? inserts { get; set; }
        public int? updates { get; set; }
        public int? deletes { get; set; }
        public int? skippeds { get; set; }
        public int? errors { get; set; }
        public ResultTransfers(int count, int? inserts, int? updates, int? deletes, int? skippeds, int? errors)
        {
            this.counts = count;
            this.inserts = inserts;
            this.updates = updates;
            this.deletes = deletes;
            this.skippeds = skippeds;
            this.errors = errors;
        }
        public void IncInsert() { if (inserts!=null) inserts++; }
        public void IncUpdate() { if (updates != null) updates++; }
        public void IncDelete() { if (deletes != null) deletes++; }
        public void IncSkipped() { if (skippeds != null) skippeds++; }
        public void IncError() { if (errors != null) errors++; }
        public int ResultInsert { get { if (this.inserts != null & this.skippeds != null) { return (int)this.inserts + (int)this.skippeds; } else return 0; } }
        public int ResultDelete { get { if (this.deletes != null & this.skippeds != null) { return (int)this.deletes + (int)this.skippeds; } else return 0; } }
        /// <summary>
        /// Обработать резултат (возращает true если была ошибка)
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool SetResultInsert(int result) 
        {
            this.result = result;
            if (result < 0) { IncError(); return true; }
            if (result == 0) { IncSkipped(); }
            if (result > 0) { IncInsert(); }
            return false;
        }
        public bool SetResultDelete(int result) 
        {
            this.result = result;
            if (result < 0) { IncError(); return true; }
            if (result == 0) { IncSkipped(); }
            if (result > 0) { IncDelete(); }
            return false;
        }
        public bool SetResultUpdate(int result) 
        {
            this.result = result;
            if (result < 0) { IncError(); return true; }
            if (result == 0) { IncSkipped(); }
            if (result > 0) { IncUpdate(); }
            return false;
        }
    }

    //public class ResultTransferWagon
    //{
    //    public int result { get; set; }
    //    public string message  { get; set; }
    //}

    public class trWagon
    {
        public int Position { get; set; }
        public int CarriageNumber { get; set; }
        public int CountryCode { get; set; }
        public float Weight { get; set; }
        public int IDCargo { get; set; }
        public string Cargo { get; set; }
        public int IDStation { get; set; }
        public string Station { get; set; }
        public int Consignee { get; set; }
        public string Operation { get; set; }
        public string CompositionIndex { get; set; }
        public DateTime DateOperation { get; set; }
        public int TrainNumber { get; set; }
        public int Conditions { get; set; }
    }

    public class trSostav
    {
        public int id { get; set; }
        public int? codecs_in_station { get; set; } // Станция получатель состава
        public int? codecs_from_station { get; set; } // Станция отравитель состава
        //public string FileName { get; set; }
        //public string CompositionIndex { get; set; }
        public DateTime DateTime_on { get; set; }        
        public DateTime DateTime_from { get; set; }
        //public int Operation { get; set; }
        //public DateTime Create { get; set; }
        //public DateTime? Close { get; set; }
        public int? ParentID { get; set; }
        public trWagon[] Wagons { get; set; }
    }

    //public class kisWagon 
    //{
    //    public int? id_wagon { get; set; }
    //    public int? id_owner_country { get; set; }   
    //    public int? id_owner { get; set; }      
    //    public int? id_shop { get; set; }
    //    public int? id_gruz { get; set; }
    //}

    public class Transfer
    {

        private eventID eventID = eventID.TransferWagons_Transfers_Transfer;
        ReferencesKIS ref_kis = new ReferencesKIS();

        public Transfer() 
        {
            //try
            //{
            //    Settings set = new Settings();
            //    set.Get_Project(this.className, this.classDescription, true); // Проверим наличие проекта
            //    //this.eventID = (int)set.GetIntSettingConfigurationManager("eventID_TransferWagons", this.className, true);
            //}
            //catch (Exception e)
            //{
            //    error_settings = true;
            //    LogRW.LogError(String.Format("[Transfer.Transfer] : Ошибка выполнения инициализации classa {0} (источник: {1}, № {2}, описание:  {3})", this.className, e.Source, e.HResult, e.Message), this.eventID);
            //}
        }

        #region Операции с списками номеров вагонов
        /// <summary>
        /// Пренадлежит указаный вагон списку вагонов
        /// </summary>
        /// <param name="num"></param>
        /// <param name="wagons"></param>
        /// <returns></returns>
        protected bool IsWagonToList(int num, int[] wagons)
        {
            if (wagons == null) return false;
            foreach (int wnum in wagons)
            {
                if (num == wnum) return true;
            }
            return false;
        }
        /// <summary>
        /// Найти и удалить номер из списка
        /// </summary>
        /// <param name="num"></param>
        /// <param name="wagons"></param>
        /// <returns></returns>
        protected bool DelWagonToList(int num, ref List<int> wagons)
        {
            int index = wagons.Count() - 1;
            while (index >= 0)
            {
                if (wagons[index] == num)
                {
                    wagons.RemoveAt(index);
                    return true;
                }
                index--;
            }
            return false;
        }

        /// <summary>
        /// Преобразовать строку список вагонов в масив номеров вагонов типа int
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        protected int[] GetWagonsToInt(string list)
        {
            if (list == null) return null;
            string[] wagons = !String.IsNullOrWhiteSpace(list) ? list.Split(';') : null;
            List<int> ints = new List<int>();
            foreach (string st in wagons)
            {
                if (!String.IsNullOrWhiteSpace(st))
                {
                    ints.Add(int.Parse(st));
                }
            }
            return ints.ToArray();
        }

        protected List<int> GetWagonsToListInt(string list) 
        {
            int[] ints = GetWagonsToInt(list);
            if (ints!=null) return ints.ToList();
            return null;
        }
        /// <summary>
        /// Преобразовать List<PromVagon> в список номеров вагонов типа string
        /// </summary>
        /// <param name="list_pv"></param>
        /// <returns></returns>
        protected string GetWagonsToString(List<PromVagon> list_pv) 
        { 
            if (list_pv == null | list_pv.Count()==0) return null;
            string res = null;
            foreach (PromVagon pv in list_pv) 
            {
                res += pv.N_VAG.ToString() + ";";
            }
            return res;
        }
        /// <summary>
        /// Преобразовать List<PromNatHist> в список номеров вагонов типа string
        /// </summary>
        /// <param name="list_nh"></param>
        /// <returns></returns>
        protected string GetWagonsToString(List<PromNatHist> list_nh) 
        { 
            if (list_nh == null | list_nh.Count()==0) return null;
            string res = null;
            foreach (PromNatHist pv in list_nh) 
            {
                res += pv.N_VAG.ToString() + ";";
            }
            return res;
        }
        /// <summary>
        /// Преобразовать List<int> в список номеров вагонов типа string
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        protected string GetWagonsToString(List<int> list) 
        { 
            if (list == null) return null;
            string res = null;
            foreach (int pv in list) 
            {
                res += pv.ToString() + ";";
            }
            return res;
        }
        /// <summary>
        /// Преобразовать List<PromVagon> в масив номеров вагонов типа int
        /// </summary>
        /// <param name="list_pv"></param>
        /// <returns></returns>
        protected int[] GetWagonsToInt(List<PromVagon> list_pv) 
        { 
            if (list_pv == null | list_pv.Count()==0) return null;
            return GetWagonsToInt(GetWagonsToString(list_pv));
        }

        protected bool DeleteExistWagon(ref List<int> list, int wag)
        {
            if (list == null) return false;
            bool Result = false;
            int index = list.Count() - 1;
            while (index >= 0)
            {
                if (list[index] == wag)
                {
                    list.RemoveAt(index);
                    Result = true;
                }
                index--;
            }
            return Result;
        }

        protected void DeleteExistWagon(ref List<int> list_new, ref List<int> list_old)
        {
            if (list_new == null & list_old == null) return;
            int index = list_new.Count() - 1;
            while (index >= 0)
            {
                if (DeleteExistWagon(ref list_old, list_new[index]))
                {
                    list_new.RemoveAt(index);
                }
                index--;
            }
        }

        #endregion

        #region trWagon
        /// <summary>
        /// Получить список номеров вагонов с trWagon[]
        /// </summary>
        /// <param name="wagons"></param>
        /// <returns></returns>
        protected int[] GetWagonsToInt(trWagon[] wagons) 
        {
            if (wagons == null | wagons.Count()==0) return null;
            List<int> res = null;
            foreach (trWagon wag in wagons)
            {
                res.Add(wag.CarriageNumber);
            }
            return res.ToArray();
        }
        /// <summary>
        /// Получить список номеров вагонов с trWagon[]
        /// </summary>
        /// <param name="wagons"></param>
        /// <returns></returns>
        protected List<int> GetWagonsToListInt(trWagon[] wagons) 
        {
            List<int> res = new List<int>();
            if (wagons != null)
            {
                foreach (trWagon wag in wagons)
                {
                    res.Add(wag.CarriageNumber);
                }
            }
            return res;
        }
        /// <summary>
        /// Вернуть trWagon по номеру вагона из спсиска trWagon[]
        /// </summary>
        /// <param name="wagons"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        protected trWagon GetWagons(trWagon[] wagons, int num) 
        {
            if (wagons == null | wagons.Count()==0) return null;
            foreach (trWagon wag in wagons)
            {
                if (wag.CarriageNumber==num) return wag;
            }
            return null;
        }
        #endregion

        #region trSostav

        #endregion

        //#region kisWagon
        //public kisWagon GetKisWagon(PromNatHist pnh)
        //{
        //    kisWagon kwag = new kisWagon();
        //    // Определим цех
        //    if (pnh.K_POL_GR != null)
        //    {
        //        kwag.id_shop = ref_kis.DefinitionIDShop((int)pnh.K_POL_GR);
        //    } 
        //    // определяем название груза           
        //    if (pnh.K_GR != null)
        //    {
        //        kwag.id_gruz = ref_kis.DefinitionIDGruzs((int)pnh.K_GR, null);
        //    }
        //    kwag.id_wagon = ref_kis.DefinitionSetIDVagon(pnh.N_VAG, DateTime.Parse(pnh.D_PR_DD.ToString() + "-" + pnh.D_PR_MM.ToString() + "-" + pnh.D_PR_YY.ToString() + " " + pnh.T_PR_HH.ToString() + ":" + pnh.T_PR_MI.ToString() + ":00", CultureInfo.CreateSpecificCulture("ru-RU")), -1, null, pnh.N_NATUR, false); 
        //    return kwag;
        //}
        //#endregion

    }



}
