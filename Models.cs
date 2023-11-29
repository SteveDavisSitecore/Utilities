using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Utilities
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FacetProperty : Attribute
    {
        public string ID { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CategoryProperty : Attribute
    {
        public string ID { get; set; }
    }
    
    public class BokusXp
    {
        public string publisher { get; set; }
        public string illustrations { get; internal set; }
        public string onix_code { get; internal set; }
        public string num_pages { get; internal set; }
        public string language_code { get; internal set; }
        public string edition { get; internal set; }
        public string content { get; internal set; }
        public List<string> authors { get; internal set; }
        public string binding { get; internal set; }
        public string language { get; internal set; }

        //public ExpandoObject facets { get; set; }
        //public List<Category> categories { get; set; }
        public BokusXp()
        {
            //facets = new ExpandoObject();
            //categories = new List<Category>();
        }
    }

    public class Category
    {
        public string code { get; set; }
        public string name { get; set; }
    }
    public class BokusProduct
    {
        public string series_part_no { get; set; }
        public string product_group { get; set; }
        public string[] authors { get; set; }
        public string illustrations { get; set; }
        public string num_pages { get; set; }
        public string onix_code { get; set; }
        public string language_code { get; set; }
        public string edition { get; set; }
        public string[] non_book_fact_words { get; set; }
        public string content { get; set; }
        public string downloadable_formats { get; set; }
        public string other_info { get; set; }
        public string num_reviews { get; set; }
        public string non_book_facets { get; set; }
        public int? play_duration { get; set; }
        public string data_source { get; set; }
        public string print_date { get; set; }
        public string sub_title { get; set; }
        public string binding { get; set; }
        public string teaser { get; set; }
        public string translator { get; set; }
        public string read_by { get; set; }
        public string language { get; set; }
        public string customer_grade { get; set; }
        public string original_title { get; set; }
        public string age_group { get; set; }
        public string non_book_categgory_level2 { get; set; }
        public string orig_lang { get; set; }
        public string play_contributor { get; set; }
        public string non_book_facts { get; set; }
        public Subject[] subjects { get; set; }
        public string num_grades { get; set; }
        public string[] non_book_category_words { get; set; }
        public string store_classification { get; set; }
        public string[] non_book_facets_words { get; set; }
        public string non_book_categgory_level3 { get; set; }
        public string issn { get; set; }
        public string series { get; set; }
        public string[] sab_codes { get; set; }
    }

    public class Subject
    {
        public Subject_Level1 subject_level1 { get; set; }
        public Subject_Level2 subject_level2 { get; set; }
        public Subject_Level3 subject_level3 { get; set; }
        public Subject_Level4 subject_level4 { get; set; }
    }

    public class Subject_Level1
    {
        public string code { get; set; }
        public string name { get; set; }
    }

    public class Subject_Level2
    {
        public string code { get; set; }
        public string name { get; set; }
    }

    public class Subject_Level3
    {
        public string name { get; set; }
        public string code { get; set; }
    }

    public class Subject_Level4
    {
        public string code { get; set; }
        public string name { get; set; }
    }

}
