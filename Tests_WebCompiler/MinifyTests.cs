﻿using NUnit.Framework;
using System.Collections.Generic;
using WebCompiler.Compile;
using WebCompiler.Configuration.Settings;

namespace Tests_WebCompiler
{
    public class MinifyTests : TestsBase
    {
        [SetUp]
        public void CreatePipeline()
        {
            pipeline = (file) => new CompilationStep(file).With(new CssMinifier(new CssMinifySettings { TermSemicolons = false }));
            input = "../../../TestCases/Css/test.css";
            output_files = new List<string> { "../../../TestCases/Css/test.min.css" };
            expected_output = "../../../TestCases/MinCss/test.min.css";
            DeleteTemporaryFiles();
        }
        [Test]
        public void CallTest() => Test();
    }
}