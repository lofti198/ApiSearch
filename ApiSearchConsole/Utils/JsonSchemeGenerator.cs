public static class JsonSchemeGenerator
{
    public static object GetJsonSchema()
    {
        return new
        {
            name = "AnswerExtractionSchema",
            schema = new
            {
                type = "object",
                properties = new
                {
                    answers = new
                    {
                        type = "array",
                        description = "List of question-answer pairs relevant to the prompt.",
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
                    }
                },
                required = new[] { "answers" }
            }
        };
    }
}
