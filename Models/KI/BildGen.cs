using Newtonsoft.Json;

namespace KaffeBot.Models.KI
{
    #pragma warning disable
    public class _12
    {
        public Inputs inputs { get; set; }
        public string class_type { get; set; }
        public Meta _meta { get; set; }
    }

    public class _15
    {
        public Inputs inputs { get; set; }
        public string class_type { get; set; }
        public Meta _meta { get; set; }
    }

    public class _16
    {
        public Inputs inputs { get; set; }
        public string class_type { get; set; }
        public Meta _meta { get; set; }
    }

    public class _2
    {
        public Inputs inputs { get; set; }
        public string class_type { get; set; }
        public Meta _meta { get; set; }
    }

    public class _3
    {
        public Inputs inputs { get; set; }
        public string class_type { get; set; }
        public Meta _meta { get; set; }
    }

    public class _4
    {
        public Inputs inputs { get; set; }
        public string class_type { get; set; }
        public Meta _meta { get; set; }
    }

    public class _5
    {
        public Inputs inputs { get; set; }
        public string class_type { get; set; }
        public Meta _meta { get; set; }
    }

    public class _6
    {
        public Inputs inputs { get; set; }
        public string class_type { get; set; }
        public Meta _meta { get; set; }
    }

    public class _7
    {
        public Inputs inputs { get; set; }
        public string class_type { get; set; }
        public Meta _meta { get; set; }
    }

    public class _8
    {
        public Inputs inputs { get; set; }
        public string class_type { get; set; }
        public Meta _meta { get; set; }
    }

    public class _9
    {
        public Inputs inputs { get; set; }
        public string class_type { get; set; }
        public Meta _meta { get; set; }
    }

    public class Inputs
    {
        public string add_noise { get; set; }
        public List<object> noise_seed { get; set; }
        public int steps { get; set; }
        public int cfg { get; set; }
        public string sampler_name { get; set; }
        public string scheduler { get; set; }
        public int start_at_step { get; set; }
        public int end_at_step { get; set; }
        public string return_with_leftover_noise { get; set; }
        public string preview_method { get; set; }
        public string vae_decode { get; set; }
        public List<object> model { get; set; }
        public List<object> positive { get; set; }
        public List<object> negative { get; set; }
        public List<object> latent_image { get; set; }
        public List<object> optional_vae { get; set; }
        public List<object> script { get; set; }
        public List<object> images { get; set; }
        public string upscale_type { get; set; }
        public string hires_ckpt_name { get; set; }
        public string latent_upscaler { get; set; }
        public string pixel_upscaler { get; set; }
        public double upscale_by { get; set; }
        public bool use_same_seed { get; set; }
        public int seed { get; set; }
        public int hires_steps { get; set; }
        public double denoise { get; set; }
        public int iterations { get; set; }
        public string use_controlnet { get; set; }
        public string control_net_name { get; set; }
        public int strength { get; set; }
        public string preprocessor { get; set; }
        public bool preprocessor_imgs { get; set; }
        public string input_mode { get; set; }
        public int lora_count { get; set; }
        public string lora_name_1 { get; set; }
        public double lora_wt_1 { get; set; }
        public int model_str_1 { get; set; }
        public int clip_str_1 { get; set; }
        public string lora_name_2 { get; set; }
        public double lora_wt_2 { get; set; }
        public int model_str_2 { get; set; }
        public int clip_str_2 { get; set; }
        public string lora_name_3 { get; set; }
        public double lora_wt_3 { get; set; }
        public int model_str_3 { get; set; }
        public int clip_str_3 { get; set; }
        public string lora_name_4 { get; set; }
        public int lora_wt_4 { get; set; }
        public int model_str_4 { get; set; }
        public int clip_str_4 { get; set; }
        public string lora_name_5 { get; set; }
        public int lora_wt_5 { get; set; }
        public int model_str_5 { get; set; }
        public int clip_str_5 { get; set; }
        public string lora_name_6 { get; set; }
        public int lora_wt_6 { get; set; }
        public int model_str_6 { get; set; }
        public int clip_str_6 { get; set; }
        public string lora_name_7 { get; set; }
        public int lora_wt_7 { get; set; }
        public int model_str_7 { get; set; }
        public int clip_str_7 { get; set; }
        public string lora_name_8 { get; set; }
        public int lora_wt_8 { get; set; }
        public int model_str_8 { get; set; }
        public int clip_str_8 { get; set; }
        public string lora_name_9 { get; set; }
        public int lora_wt_9 { get; set; }
        public int model_str_9 { get; set; }
        public int clip_str_9 { get; set; }
        public string lora_name_10 { get; set; }
        public int lora_wt_10 { get; set; }
        public int model_str_10 { get; set; }
        public int clip_str_10 { get; set; }
        public string lora_name_11 { get; set; }
        public int lora_wt_11 { get; set; }
        public int model_str_11 { get; set; }
        public int clip_str_11 { get; set; }
        public string lora_name_12 { get; set; }
        public int lora_wt_12 { get; set; }
        public int model_str_12 { get; set; }
        public int clip_str_12 { get; set; }
        public string lora_name_13 { get; set; }
        public int lora_wt_13 { get; set; }
        public int model_str_13 { get; set; }
        public int clip_str_13 { get; set; }
        public string lora_name_14 { get; set; }
        public int lora_wt_14 { get; set; }
        public int model_str_14 { get; set; }
        public int clip_str_14 { get; set; }
        public string lora_name_15 { get; set; }
        public int lora_wt_15 { get; set; }
        public int model_str_15 { get; set; }
        public int clip_str_15 { get; set; }
        public string lora_name_16 { get; set; }
        public int lora_wt_16 { get; set; }
        public int model_str_16 { get; set; }
        public int clip_str_16 { get; set; }
        public string lora_name_17 { get; set; }
        public int lora_wt_17 { get; set; }
        public int model_str_17 { get; set; }
        public int clip_str_17 { get; set; }
        public string lora_name_18 { get; set; }
        public int lora_wt_18 { get; set; }
        public int model_str_18 { get; set; }
        public int clip_str_18 { get; set; }
        public string lora_name_19 { get; set; }
        public int lora_wt_19 { get; set; }
        public int model_str_19 { get; set; }
        public int clip_str_19 { get; set; }
        public string lora_name_20 { get; set; }
        public int lora_wt_20 { get; set; }
        public int model_str_20 { get; set; }
        public int clip_str_20 { get; set; }
        public string lora_name_21 { get; set; }
        public int lora_wt_21 { get; set; }
        public int model_str_21 { get; set; }
        public int clip_str_21 { get; set; }
        public string lora_name_22 { get; set; }
        public int lora_wt_22 { get; set; }
        public int model_str_22 { get; set; }
        public int clip_str_22 { get; set; }
        public string lora_name_23 { get; set; }
        public int lora_wt_23 { get; set; }
        public int model_str_23 { get; set; }
        public int clip_str_23 { get; set; }
        public string lora_name_24 { get; set; }
        public int lora_wt_24 { get; set; }
        public int model_str_24 { get; set; }
        public int clip_str_24 { get; set; }
        public string lora_name_25 { get; set; }
        public int lora_wt_25 { get; set; }
        public int model_str_25 { get; set; }
        public int clip_str_25 { get; set; }
        public string lora_name_26 { get; set; }
        public int lora_wt_26 { get; set; }
        public int model_str_26 { get; set; }
        public int clip_str_26 { get; set; }
        public string lora_name_27 { get; set; }
        public int lora_wt_27 { get; set; }
        public int model_str_27 { get; set; }
        public int clip_str_27 { get; set; }
        public string lora_name_28 { get; set; }
        public int lora_wt_28 { get; set; }
        public int model_str_28 { get; set; }
        public int clip_str_28 { get; set; }
        public string lora_name_29 { get; set; }
        public int lora_wt_29 { get; set; }
        public int model_str_29 { get; set; }
        public int clip_str_29 { get; set; }
        public string lora_name_30 { get; set; }
        public int lora_wt_30 { get; set; }
        public int model_str_30 { get; set; }
        public int clip_str_30 { get; set; }
        public string lora_name_31 { get; set; }
        public int lora_wt_31 { get; set; }
        public int model_str_31 { get; set; }
        public int clip_str_31 { get; set; }
        public string lora_name_32 { get; set; }
        public int lora_wt_32 { get; set; }
        public int model_str_32 { get; set; }
        public int clip_str_32 { get; set; }
        public string lora_name_33 { get; set; }
        public int lora_wt_33 { get; set; }
        public int model_str_33 { get; set; }
        public int clip_str_33 { get; set; }
        public string lora_name_34 { get; set; }
        public int lora_wt_34 { get; set; }
        public int model_str_34 { get; set; }
        public int clip_str_34 { get; set; }
        public string lora_name_35 { get; set; }
        public int lora_wt_35 { get; set; }
        public int model_str_35 { get; set; }
        public int clip_str_35 { get; set; }
        public string lora_name_36 { get; set; }
        public int lora_wt_36 { get; set; }
        public int model_str_36 { get; set; }
        public int clip_str_36 { get; set; }
        public string lora_name_37 { get; set; }
        public int lora_wt_37 { get; set; }
        public int model_str_37 { get; set; }
        public int clip_str_37 { get; set; }
        public string lora_name_38 { get; set; }
        public int lora_wt_38 { get; set; }
        public int model_str_38 { get; set; }
        public int clip_str_38 { get; set; }
        public string lora_name_39 { get; set; }
        public int lora_wt_39 { get; set; }
        public int model_str_39 { get; set; }
        public int clip_str_39 { get; set; }
        public string lora_name_40 { get; set; }
        public int lora_wt_40 { get; set; }
        public int model_str_40 { get; set; }
        public int clip_str_40 { get; set; }
        public string lora_name_41 { get; set; }
        public int lora_wt_41 { get; set; }
        public int model_str_41 { get; set; }
        public int clip_str_41 { get; set; }
        public string lora_name_42 { get; set; }
        public int lora_wt_42 { get; set; }
        public int model_str_42 { get; set; }
        public int clip_str_42 { get; set; }
        public string lora_name_43 { get; set; }
        public int lora_wt_43 { get; set; }
        public int model_str_43 { get; set; }
        public int clip_str_43 { get; set; }
        public string lora_name_44 { get; set; }
        public int lora_wt_44 { get; set; }
        public int model_str_44 { get; set; }
        public int clip_str_44 { get; set; }
        public string lora_name_45 { get; set; }
        public int lora_wt_45 { get; set; }
        public int model_str_45 { get; set; }
        public int clip_str_45 { get; set; }
        public string lora_name_46 { get; set; }
        public int lora_wt_46 { get; set; }
        public int model_str_46 { get; set; }
        public int clip_str_46 { get; set; }
        public string lora_name_47 { get; set; }
        public int lora_wt_47 { get; set; }
        public int model_str_47 { get; set; }
        public int clip_str_47 { get; set; }
        public string lora_name_48 { get; set; }
        public int lora_wt_48 { get; set; }
        public int model_str_48 { get; set; }
        public int clip_str_48 { get; set; }
        public string lora_name_49 { get; set; }
        public int lora_wt_49 { get; set; }
        public int model_str_49 { get; set; }
        public int clip_str_49 { get; set; }
        public List<object> clip { get; set; }
        public List<object> lora_stack { get; set; }
        public string ckpt_name { get; set; }
        public string text { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int batch_size { get; set; }
        public string vae_name { get; set; }
    }

    public class Meta
    {
        public string title { get; set; }
    }

    /// <summary>
    /// Json Class zur Bildgenerierung
    /// </summary>
    public sealed class BildGen
    {
        [JsonProperty("2")]
        public _2 _2 { get; set; }

        [JsonProperty("3")]
        public _3 _3 { get; set; }

        [JsonProperty("4")]
        public _4 _4 { get; set; }

        [JsonProperty("5")]
        public _5 _5 { get; set; }

        [JsonProperty("6")]
        public _6 _6 { get; set; }

        [JsonProperty("7")]
        public _7 _7 { get; set; }

        [JsonProperty("8")]
        public _8 _8 { get; set; }

        [JsonProperty("9")]
        public _9 _9 { get; set; }

        [JsonProperty("12")]
        public _12 _12 { get; set; }

        [JsonProperty("15")]
        public _15 _15 { get; set; }

        [JsonProperty("16")]
        public _16 _16 { get; set; }
    }
    #pragma warning restore IDE1006
}