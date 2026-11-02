using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCompetency.Api.Migrations;

public partial class AlignCvDraftRubricWithThesis : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Preserve any draft edits already made by administrators. Update only untouched seed rows.
        migrationBuilder.Sql("""
            UPDATE competency_framework_versions SET "Source" = 'Proposed indicators aligned to thesis v8.1 sections 1.2 and 2.3; Appendices A/B are listed as planned but contain no full text in this file. Expert review pending.'
            WHERE "Id" = 1 AND "Status" = 'Draft' AND "Source" = 'Proposed from repository Phase 0 catalogue and thesis roadmap; not a verbatim thesis appendix. Expert review pending.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.',
                "Example" = 'Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.', "InsufficientEvidence" = 'Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.'
            WHERE "Id" = 1 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'No valid span supports any indicator.'
                AND "Example" = 'Skills: ChatGPT, Python.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.',
                "Example" = 'Hoàn thành bài tập giải thích khác biệt giữa luật lập trình và dự đoán AI, kèm sản phẩm nộp.', "InsufficientEvidence" = 'Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.'
            WHERE "Id" = 2 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Explain AI in a specific project'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.',
                "Example" = 'Trong dự án phân loại văn bản, tự chọn phương pháp học máy và giải thích kết quả đánh giá.', "InsufficientEvidence" = 'Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.'
            WHERE "Id" = 3 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Describe completed model development steps'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.',
                "Example" = 'Thiết kế quy trình so sánh hai mô hình, phân tích đánh đổi dữ liệu và phương pháp cho nhóm triển khai.', "InsufficientEvidence" = 'Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.'
            WHERE "Id" = 4 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Compare algorithms using measured criteria'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.',
                "Example" = 'Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.', "InsufficientEvidence" = 'Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.'
            WHERE "Id" = 5 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'No valid span supports any indicator.'
                AND "Example" = 'Skills: ChatGPT, Python.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.',
                "Example" = 'Trong học phần dữ liệu, làm sạch dữ liệu trùng và nộp bảng kết quả trước/sau.', "InsufficientEvidence" = 'Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.'
            WHERE "Id" = 6 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Collect data from identified sources'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.',
                "Example" = 'Tự thiết kế bước kiểm tra dữ liệu thiếu cho dự án dự báo và đối chiếu chất lượng đầu ra.', "InsufficientEvidence" = 'Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.'
            WHERE "Id" = 7 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Evaluate data quality against criteria'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.',
                "Example" = 'Thiết kế pipeline dữ liệu có kiểm soát chất lượng và kiểm tra rò rỉ giữa các tập huấn luyện/kiểm định.', "InsufficientEvidence" = 'Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.'
            WHERE "Id" = 8 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Design a data pipeline with quality checks'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.',
                "Example" = 'Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.', "InsufficientEvidence" = 'Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.'
            WHERE "Id" = 9 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'No valid span supports any indicator.'
                AND "Example" = 'Skills: ChatGPT, Python.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.',
                "Example" = 'Đối chiếu câu trả lời của trợ lý AI với một nguồn độc lập trong bài tập và ghi lại sai khác.', "InsufficientEvidence" = 'Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.'
            WHERE "Id" = 10 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Identify sources of AI generated information'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.',
                "Example" = 'Tự lập tiêu chí kiểm chứng, so sánh đầu ra hai công cụ AI và phân tích nguyên nhân lỗi.', "InsufficientEvidence" = 'Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.'
            WHERE "Id" = 11 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Compare outputs from multiple AI tools'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.',
                "Example" = 'Thiết kế quy trình kiểm chứng đầu ra AI cho nhóm, dùng phản ví dụ để đánh giá độ tin cậy.', "InsufficientEvidence" = 'Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.'
            WHERE "Id" = 12 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Design a reproducible AI verification workflow'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.',
                "Example" = 'Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.', "InsufficientEvidence" = 'Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.'
            WHERE "Id" = 13 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'No valid span supports any indicator.'
                AND "Example" = 'Skills: ChatGPT, Python.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.',
                "Example" = 'Dùng trợ lý AI để soạn bản nháp cho một tác vụ cụ thể và kiểm tra sản phẩm trước khi nộp.', "InsufficientEvidence" = 'Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.'
            WHERE "Id" = 14 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Apply AI to a defined task'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.',
                "Example" = 'Tự tích hợp API AI vào quy trình phân loại yêu cầu và so sánh kết quả với cách làm thủ công.', "InsufficientEvidence" = 'Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.'
            WHERE "Id" = 15 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Integrate AI into a real workflow'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.',
                "Example" = 'Thiết kế giải pháp AI cho quy trình hỗ trợ, đánh giá chất lượng, chi phí và khả năng vận hành.', "InsufficientEvidence" = 'Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.'
            WHERE "Id" = 16 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Design an AI solution meeting practical constraints'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.',
                "Example" = 'Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.', "InsufficientEvidence" = 'Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.'
            WHERE "Id" = 17 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'No valid span supports any indicator.'
                AND "Example" = 'Skills: ChatGPT, Python.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.',
                "Example" = 'Trong bài tập ứng dụng AI, nhận diện dữ liệu cá nhân và mô tả biện pháp che thông tin.', "InsufficientEvidence" = 'Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.'
            WHERE "Id" = 18 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Identify personal data in an AI task'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.',
                "Example" = 'Tự đánh giá rủi ro thiên lệch của hệ thống theo nhóm bị ảnh hưởng và kiểm tra biện pháp giảm thiểu.', "InsufficientEvidence" = 'Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.'
            WHERE "Id" = 19 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Apply data protection measures when using AI'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.',
                "Example" = 'Xây chính sách sử dụng AI cho đơn vị, thiết kế giám sát rủi ro và đánh giá hiệu quả biện pháp bảo vệ.', "InsufficientEvidence" = 'Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.'
            WHERE "Id" = 20 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Develop responsible AI usage policies'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.',
                "Example" = 'Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.', "InsufficientEvidence" = 'Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.'
            WHERE "Id" = 21 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'No valid span supports any indicator.'
                AND "Example" = 'Skills: ChatGPT, Python.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.',
                "Example" = 'Dùng công cụ AI cho một tác vụ cụ thể, kiểm tra đầu ra và nộp sản phẩm đã sửa.', "InsufficientEvidence" = 'Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.'
            WHERE "Id" = 22 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Use an AI tool for a specific task'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.',
                "Example" = 'Tự chọn và tích hợp công cụ AI vào luồng hỗ trợ, đo lỗi trước và sau khi áp dụng.', "InsufficientEvidence" = 'Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.'
            WHERE "Id" = 23 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Compare tools against work requirements'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.',
                "Example" = 'Thiết kế luồng phối hợp nhiều công cụ AI cho nhóm và đánh giá hiệu quả, chi phí, khả năng tiếp cận.', "InsufficientEvidence" = 'Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.'
            WHERE "Id" = 24 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Design coordination of multiple AI tools'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.',
                "Example" = 'Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.', "InsufficientEvidence" = 'Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.'
            WHERE "Id" = 25 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'No valid span supports any indicator.'
                AND "Example" = 'Skills: ChatGPT, Python.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.',
                "Example" = 'Hoàn thành học phần về AI với sản phẩm mô tả nhiệm vụ công việc có thể thay đổi.', "InsufficientEvidence" = 'Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.'
            WHERE "Id" = 26 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Identify tasks that AI may change'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.',
                "Example" = 'Tự phân tích khoảng cách kỹ năng trong nhóm và thử nghiệm kế hoạch đào tạo dùng AI.', "InsufficientEvidence" = 'Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.'
            WHERE "Id" = 27 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Analyze role changes in a workflow'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.',
                "Example" = 'Xây chiến lược thích ứng AI cho tổ chức, theo dõi tác động tới vai trò và kết quả đào tạo.', "InsufficientEvidence" = 'Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.'
            WHERE "Id" = 28 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.'
                AND "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Develop an organizational AI adaptation strategy'' and states the task context and candidate contribution. This prompt is not evidence itself.'
                AND "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Restore only unchanged thesis-aligned seed rows; preserve later draft edits.
        migrationBuilder.Sql("""
            UPDATE competency_framework_versions SET "Source" = 'Proposed from repository Phase 0 catalogue and thesis roadmap; not a verbatim thesis appendix. Expert review pending.'
            WHERE "Id" = 1 AND "Status" = 'Draft' AND "Source" = 'Proposed indicators aligned to thesis v8.1 sections 1.2 and 2.3; Appendices A/B are listed as planned but contain no full text in this file. Expert review pending.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'No valid span supports any indicator.',
                "Example" = 'Skills: ChatGPT, Python.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 1 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.'
                AND "Example" = 'Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.'
                AND "InsufficientEvidence" = 'Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Explain AI in a specific project'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 2 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.'
                AND "Example" = 'Hoàn thành bài tập giải thích khác biệt giữa luật lập trình và dự đoán AI, kèm sản phẩm nộp.'
                AND "InsufficientEvidence" = 'Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Describe completed model development steps'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 3 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.'
                AND "Example" = 'Trong dự án phân loại văn bản, tự chọn phương pháp học máy và giải thích kết quả đánh giá.'
                AND "InsufficientEvidence" = 'Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Compare algorithms using measured criteria'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 4 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.'
                AND "Example" = 'Thiết kế quy trình so sánh hai mô hình, phân tích đánh đổi dữ liệu và phương pháp cho nhóm triển khai.'
                AND "InsufficientEvidence" = 'Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'No valid span supports any indicator.',
                "Example" = 'Skills: ChatGPT, Python.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 5 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.'
                AND "Example" = 'Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.'
                AND "InsufficientEvidence" = 'Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Collect data from identified sources'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 6 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.'
                AND "Example" = 'Trong học phần dữ liệu, làm sạch dữ liệu trùng và nộp bảng kết quả trước/sau.'
                AND "InsufficientEvidence" = 'Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Evaluate data quality against criteria'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 7 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.'
                AND "Example" = 'Tự thiết kế bước kiểm tra dữ liệu thiếu cho dự án dự báo và đối chiếu chất lượng đầu ra.'
                AND "InsufficientEvidence" = 'Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Design a data pipeline with quality checks'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 8 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.'
                AND "Example" = 'Thiết kế pipeline dữ liệu có kiểm soát chất lượng và kiểm tra rò rỉ giữa các tập huấn luyện/kiểm định.'
                AND "InsufficientEvidence" = 'Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'No valid span supports any indicator.',
                "Example" = 'Skills: ChatGPT, Python.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 9 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.'
                AND "Example" = 'Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.'
                AND "InsufficientEvidence" = 'Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Identify sources of AI generated information'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 10 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.'
                AND "Example" = 'Đối chiếu câu trả lời của trợ lý AI với một nguồn độc lập trong bài tập và ghi lại sai khác.'
                AND "InsufficientEvidence" = 'Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Compare outputs from multiple AI tools'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 11 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.'
                AND "Example" = 'Tự lập tiêu chí kiểm chứng, so sánh đầu ra hai công cụ AI và phân tích nguyên nhân lỗi.'
                AND "InsufficientEvidence" = 'Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Design a reproducible AI verification workflow'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 12 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.'
                AND "Example" = 'Thiết kế quy trình kiểm chứng đầu ra AI cho nhóm, dùng phản ví dụ để đánh giá độ tin cậy.'
                AND "InsufficientEvidence" = 'Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'No valid span supports any indicator.',
                "Example" = 'Skills: ChatGPT, Python.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 13 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.'
                AND "Example" = 'Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.'
                AND "InsufficientEvidence" = 'Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Apply AI to a defined task'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 14 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.'
                AND "Example" = 'Dùng trợ lý AI để soạn bản nháp cho một tác vụ cụ thể và kiểm tra sản phẩm trước khi nộp.'
                AND "InsufficientEvidence" = 'Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Integrate AI into a real workflow'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 15 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.'
                AND "Example" = 'Tự tích hợp API AI vào quy trình phân loại yêu cầu và so sánh kết quả với cách làm thủ công.'
                AND "InsufficientEvidence" = 'Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Design an AI solution meeting practical constraints'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 16 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.'
                AND "Example" = 'Thiết kế giải pháp AI cho quy trình hỗ trợ, đánh giá chất lượng, chi phí và khả năng vận hành.'
                AND "InsufficientEvidence" = 'Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'No valid span supports any indicator.',
                "Example" = 'Skills: ChatGPT, Python.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 17 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.'
                AND "Example" = 'Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.'
                AND "InsufficientEvidence" = 'Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Identify personal data in an AI task'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 18 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.'
                AND "Example" = 'Trong bài tập ứng dụng AI, nhận diện dữ liệu cá nhân và mô tả biện pháp che thông tin.'
                AND "InsufficientEvidence" = 'Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Apply data protection measures when using AI'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 19 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.'
                AND "Example" = 'Tự đánh giá rủi ro thiên lệch của hệ thống theo nhóm bị ảnh hưởng và kiểm tra biện pháp giảm thiểu.'
                AND "InsufficientEvidence" = 'Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Develop responsible AI usage policies'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 20 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.'
                AND "Example" = 'Xây chính sách sử dụng AI cho đơn vị, thiết kế giám sát rủi ro và đánh giá hiệu quả biện pháp bảo vệ.'
                AND "InsufficientEvidence" = 'Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'No valid span supports any indicator.',
                "Example" = 'Skills: ChatGPT, Python.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 21 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.'
                AND "Example" = 'Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.'
                AND "InsufficientEvidence" = 'Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Use an AI tool for a specific task'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 22 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.'
                AND "Example" = 'Dùng công cụ AI cho một tác vụ cụ thể, kiểm tra đầu ra và nộp sản phẩm đã sửa.'
                AND "InsufficientEvidence" = 'Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Compare tools against work requirements'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 23 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.'
                AND "Example" = 'Tự chọn và tích hợp công cụ AI vào luồng hỗ trợ, đo lỗi trước và sau khi áp dụng.'
                AND "InsufficientEvidence" = 'Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Design coordination of multiple AI tools'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 24 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.'
                AND "Example" = 'Thiết kế luồng phối hợp nhiều công cụ AI cho nhóm và đánh giá hiệu quả, chi phí, khả năng tiếp cận.'
                AND "InsufficientEvidence" = 'Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'No valid span supports any indicator.',
                "Example" = 'Skills: ChatGPT, Python.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 25 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Không có span trực tiếp phù hợp hoặc chỉ có tuyên bố chung. Không suy Basic chỉ vì CV im lặng.'
                AND "Example" = 'Chỉ ghi “ChatGPT, Python” trong mục Kỹ năng và không mô tả tác vụ hoặc sản phẩm.'
                AND "InsufficientEvidence" = 'Nếu có span cụ thể đáp ứng một chỉ báo mức 1–3 thì không giữ mức 0; chuyển sang mức tương ứng hoặc phân xử.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 1 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Identify tasks that AI may change'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 26 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy hiểu hoặc sử dụng AI ở phạm vi giới hạn, có bối cảnh: học phần/chứng chỉ có sản phẩm quan sát được hoặc dùng công cụ cho tác vụ cụ thể.'
                AND "Example" = 'Hoàn thành học phần về AI với sản phẩm mô tả nhiệm vụ công việc có thể thay đổi.'
                AND "InsufficientEvidence" = 'Chỉ liệt kê tên công cụ, chức danh, tự đánh giá hoặc chứng chỉ không có sản phẩm; không có bối cảnh hay hành động cụ thể.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 2 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Analyze role changes in a workflow'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 27 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy ứng viên vận dụng độc lập trong dự án/nhiệm vụ có bối cảnh, kèm quyết định kỹ thuật hoặc kiểm chứng kết quả và vai trò cá nhân.'
                AND "Example" = 'Tự phân tích khoảng cách kỹ năng trong nhóm và thử nghiệm kế hoạch đào tạo dùng AI.'
                AND "InsufficientEvidence" = 'Mô tả nhiệm vụ mơ hồ, chỉ nói thành tích của nhóm, không nêu vai trò cá nhân hoặc quyết định/kiểm chứng độc lập.';

            UPDATE cv_rubric_rules SET "AcceptedEvidence" = 'A concrete action matching a level 3 indicator, with task context and candidate contribution. At least one exact source span is required.',
                "Example" = 'Synthetic annotation prompt: find a CV passage that demonstrates ''Develop an organizational AI adaptation strategy'' and states the task context and candidate contribution. This prompt is not evidence itself.', "InsufficientEvidence" = 'Tool lists, job titles, self ratings, course titles alone, or vague claims without a concrete action.'
            WHERE "Id" = 28 AND "FrameworkVersionId" = 1
                AND EXISTS (SELECT 1 FROM competency_framework_versions WHERE "Id" = 1 AND "Status" = 'Draft')
                AND "AcceptedEvidence" = 'Có ít nhất một span cho thấy đánh giá, thiết kế, dẫn dắt, tối ưu hoặc chuẩn hóa ở phạm vi rộng; nêu trách nhiệm, sản phẩm hoặc kết quả quan sát được.'
                AND "Example" = 'Xây chiến lược thích ứng AI cho tổ chức, theo dõi tác động tới vai trò và kết quả đào tạo.'
                AND "InsufficientEvidence" = 'Chỉ có thâm niên, chức danh hay số liệu rời rạc mà thiếu hành vi đánh giá/thiết kế/dẫn dắt và phạm vi trách nhiệm.';
            """);
    }
}
