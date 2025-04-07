namespace ApiSearchConsole.Utils
{
    public static class JsonSchemeGenerator
    {
        public static object GetJsonScheme()
        {
            return new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        originalQuestion = new
                        {
                            type = "string",
                            description = "The part of the original prompt to which this answer refers."
                        },
                        answer = new
                        {
                            type = "string",
                            description = "The answer extracted from the text."
                        }
                    },
                    required = new[] { "originalQuestion", "answer" }
                }
            };
        }

    }

}
