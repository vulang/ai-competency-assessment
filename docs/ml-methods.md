# Phương Pháp Học Máy Áp Dụng trong Hệ Thống Đánh Giá Năng Lực AI

> **Phạm vi:** Tài liệu này mô tả các phương pháp học máy và tâm trắc học được tích hợp vào nền tảng **AI Competency Assessment** nhằm nâng cao độ chính xác của đánh giá năng lực, cải tiến engine bài test thích ứng, và tạo hồ sơ năng lực đa chiều cho ứng viên.

---

## Mục Lục

1. [Item Response Theory (IRT)](#1-item-response-theory-irt)
2. [Computerized Adaptive Testing (CAT)](#2-computerized-adaptive-testing-cat)
3. [Bayesian Knowledge Tracing (BKT)](#3-bayesian-knowledge-tracing-bkt)
4. [Competency Profile Aggregation](#4-competency-profile-aggregation)
5. [Tham Số Mặc Định & Chiến Lược Calibration](#5-tham-số-mặc-định--chiến-lược-calibration)
6. [Tài Liệu Tham Khảo](#6-tài-liệu-tham-khảo)

---

## 1. Item Response Theory (IRT)

### Tổng Quan

**Item Response Theory (Lý thuyết Phản hồi Câu hỏi)** là một khung tâm trắc học mô hình hóa xác suất một ứng viên trả lời đúng một câu hỏi dựa trên *năng lực ẩn* (latent ability) của họ và *đặc tính* của câu hỏi đó. IRT vượt trội hơn điểm thô (raw score) vì nó tách biệt được năng lực thực sự của người thi khỏi độ khó của bộ câu hỏi cụ thể.

### Mô Hình Được Chọn: 3-Parameter Logistic (3PL)

Hệ thống sử dụng mô hình **3PL** vì bộ câu hỏi bao gồm các loại MCQ có yếu tố đoán ngẫu nhiên.

```
P(X = 1 | θ) = c + (1 - c) / (1 + exp(-a(θ - b)))
```

| Tham số | Ký hiệu | Phạm vi | Ý nghĩa |
|---|---|---|---|
| Ability (năng lực) | `θ` (theta) | `[-3, +3]` | Năng lực ẩn của ứng viên, phân phối chuẩn N(0,1) |
| Discrimination (phân biệt) | `a` | `[0.5, 2.5]` | Khả năng câu hỏi phân biệt giữa ứng viên giỏi và yếu |
| Difficulty (độ khó) | `b` | `[-3, +3]` | Giá trị θ tại đó P = 0.5 (bỏ qua c) |
| Pseudo-guessing (đoán) | `c` | `[0.0, 0.33]` | Xác suất đoán đúng ngẫu nhiên (thường = 1/số lựa chọn) |

### Ước Lượng Năng Lực (θ Estimation)

| Phương pháp | Mô tả | Áp dụng |
|---|---|---|
| **MLE** (Maximum Likelihood) | Tối đa hóa hàm log-likelihood | Khi có ≥5 câu trả lời |
| **EAP** (Expected A Posteriori) | MLE + prior phân phối chuẩn N(0,1) | Khi ít câu hỏi, tránh extreme values |
| **MAP** (Maximum A Posteriori) | Bayesian với prior | Khi cần regularization |

**Triển khai:** `scipy.optimize.minimize` với hàm negative log-likelihood.

### Ánh Xạ Từ `difficulty_level` Sang `b` (Mặc Định)

```
difficulty_level 1  →  b = -2.0  (rất dễ)
difficulty_level 2  →  b = -1.0  (dễ)
difficulty_level 3  →  b =  0.0  (trung bình)
difficulty_level 4  →  b =  1.0  (khó)
difficulty_level 5  →  b =  2.0  (rất khó)
```

### Hiệu Chỉnh Tham Số (Calibration)

Khi tích lũy đủ **≥200 responses** cho một câu hỏi, hệ thống chạy **EM Algorithm (Expectation-Maximization)** để hiệu chỉnh tham số `(a, b, c)` từ dữ liệu thực tế. Kết quả lưu vào bảng `question_irt_params`.

---

## 2. Computerized Adaptive Testing (CAT)

### Tổng Quan

**CAT** là kỹ thuật chọn câu hỏi *linh hoạt theo thời gian thực* dựa trên ước lượng năng lực θ hiện tại của ứng viên, thay vì cố định trước bộ câu hỏi. Kết quả là bài test ngắn hơn (~30–50% ít câu hơn) nhưng độ chính xác tương đương.

### Tiêu Chí Chọn Câu Hỏi: Maximum Fisher Information

Câu hỏi tiếp theo được chọn là câu có **thông tin Fisher cực đại** tại θ ước lượng hiện tại:

```
I(θ) = a² × [P(θ) - c]² / [(1 - c)² × P(θ) × (1 - P(θ))]
```

Câu hỏi tối ưu: `argmax_j I_j(θ_current)` trong tập câu hỏi chưa được hỏi.

### Điều Kiện Dừng (Stopping Rules)

| Điều kiện | Ngưỡng |
|---|---|
| Số câu tối đa | `MaxQuestions = 10` (configurable) |
| Sai số chuẩn đủ nhỏ | `SE(θ) < 0.3` |
| Hết câu hỏi phù hợp | Pool exhausted |

### So Sánh: Trước vs. Sau Cải Tiến

| | Trước (hiện tại) | Sau (IRT-CAT) |
|---|---|---|
| Chọn câu hỏi | `abs(difficulty - θ)` nhỏ nhất | Max Fisher Information |
| Cập nhật θ | `θ ± 1` cố định | MLE/EAP trên toàn bộ response history |
| Phạm vi θ | `[1, 10]` (tùy ý) | `[-3, +3]` (logit scale chuẩn) |
| Điểm cuối | `CurrentAbilityEstimate` thô | θ với confidence interval `SE(θ)` |

---

## 3. Bayesian Knowledge Tracing (BKT)

### Tổng Quan

**BKT** là mô hình Hidden Markov (HMM) 2 trạng thái dùng để theo dõi xác suất ứng viên *đã thành thạo* (`mastered = true/false`) một kỹ năng cụ thể, cập nhật theo từng lần thực hành. BKT hoạt động ở cấp độ **skill** (tương ứng bảng `skills` trong DB), không phải ở cấp độ câu hỏi.

### 4 Tham Số BKT

| Tham số | Ký hiệu | Giá trị mặc định | Ý nghĩa |
|---|---|---|---|
| Prior mastery | `P(L₀)` | `0.3` | Xác suất biết kỹ năng trước khi bắt đầu |
| Learn (transition) | `P(T)` | `0.1` | Xác suất học được kỹ năng sau mỗi câu trả lời |
| Slip | `P(S)` | `0.1` | Xác suất sai dù đã thành thạo (lỗi bất cẩn) |
| Guess | `P(G)` | `0.25` | Xác suất đúng dù chưa thành thạo (đoán) |

### Công Thức Cập Nhật Bayesian

**Sau câu trả lời đúng:**
```
P(Lₙ | correct) = P(Lₙ₋₁)(1 - P(S)) / [P(Lₙ₋₁)(1 - P(S)) + (1 - P(Lₙ₋₁)) × P(G)]
```

**Sau câu trả lời sai:**
```
P(Lₙ | wrong) = P(Lₙ₋₁) × P(S) / [P(Lₙ₋₁) × P(S) + (1 - P(Lₙ₋₁)) × (1 - P(G))]
```

**Sau đó áp dụng transition:**
```
P(Lₙ₊₁) = P(Lₙ) + (1 - P(Lₙ)) × P(T)
```

### Ngưỡng Thành Thạo (Mastery Threshold)

Một kỹ năng được xem là **đã thành thạo** khi `P(Lₙ) ≥ 0.95`. Thông tin này được dùng để:
- Hiển thị thanh tiến độ per-skill trong kết quả bài test
- Đề xuất kỹ năng cần ôn tập
- Tổng hợp vào Competency Profile

### Lưu Trạng Thái

Mastery state được persist vào bảng `user_skill_mastery (user_id, skill_id)` và cập nhật sau mỗi lần ứng viên hoàn thành bài test có câu hỏi thuộc skill đó.

---

## 4. Competency Profile Aggregation

### Tổng Quan

**Competency Profile** là hồ sơ năng lực tổng hợp 7 chiều, kết hợp:
- **IRT θ** — năng lực tổng quát (global ability)
- **BKT mastery** — trạng thái thành thạo từng skill
- **Domain weights** — trọng số domain từ bảng `domains.weight`

### Công Thức Tổng Hợp

```
domain_score(d) = Σ [mastery(skill_i) × weight(skill_i)] / Σ weight(skill_i)
                  for skill_i in domain d

competency_score(d) = normalize(domain_score(d) × irt_boost(θ))

irt_boost(θ) = 1 + 0.1 × θ   # IRT θ điều chỉnh nhẹ domain scores
```

**Chuẩn hóa về thang 0–100:**
```
competency_score_pct = clip(domain_score × 100, 0, 100)
```

### 7 Chiều Năng Lực

| # | Domain | Mô tả |
|---|---|---|
| 1 | **AI Fundamentals** | Khái niệm cơ bản về AI/ML |
| 2 | **Data** | Xử lý dữ liệu, data literacy |
| 3 | **Critical Thinking** | Tư duy phản biện với AI |
| 4 | **AI Use Cases** | Ứng dụng AI trong công việc |
| 5 | **AI Ethics** | Đạo đức và an toàn AI |
| 6 | **AI Tools** | Sử dụng các công cụ AI |
| 7 | **Future of Work** | Tương lai việc làm với AI |

### Xếp Loại Tổng Thể

| Ngưỡng θ | Xếp loại | Mô tả |
|---|---|---|
| `θ < -1.0` | Foundation (Nền tảng) | Đang hình thành năng lực cơ bản |
| `-1.0 ≤ θ < 1.0` | Apply (Ứng dụng) | Có thể áp dụng AI trong công việc |
| `θ ≥ 1.0` | Create (Sáng tạo) | Có thể thiết kế và tối ưu giải pháp AI |

---

## 5. Tham Số Mặc Định & Chiến Lược Calibration

### Bootstrap Parameters (Trước Khi Có Đủ Data)

```json
{
  "irt": {
    "a_default": 1.0,
    "c_default": 0.25,
    "theta_init": 0.0,
    "theta_prior_mean": 0.0,
    "theta_prior_sd": 1.0
  },
  "bkt": {
    "p_l0": 0.3,
    "p_t": 0.1,
    "p_s": 0.1,
    "p_g": 0.25,
    "mastery_threshold": 0.95
  },
  "cat": {
    "max_questions": 10,
    "se_stopping_threshold": 0.3,
    "theta_min": -3.0,
    "theta_max": 3.0
  }
}
```

### Lịch Trình Calibration

| Giai đoạn | Điều kiện | Hành động |
|---|---|---|
| **Phase 0** | < 200 responses/question | Dùng tham số mặc định, `b` ánh xạ từ `difficulty_level` |
| **Phase 1** | ≥ 200 responses/question | Chạy EM calibration, cập nhật `question_irt_params` |
| **Phase 2** | ≥ 1000 responses tổng | Joint calibration, kiểm tra model fit (RMSEA) |
| **Ongoing** | Hàng tuần (cron job) | Re-calibrate và flag câu hỏi có DIF (Differential Item Functioning) |

---

## 6. Tài Liệu Tham Khảo

### Sách & Giáo Trình Nền Tảng

| # | Tài liệu | Tác giả | Năm | Ghi chú |
|---|---|---|---|---|
| [1] | *Item Response Theory for Psychologists* | Embretson & Reise | 2000 | Giáo trình IRT chuẩn, chương 3–5 về 3PL |
| [2] | *The Basics of Item Response Theory Using R* | Baker & Kim | 2017 | Thực hành IRT với code mẫu |
| [3] | *Computerized Adaptive Testing: A Primer* | Wainer et al. | 2000 | Nền tảng lý thuyết CAT |
| [4] | *Elements of Adaptive Testing* | van der Linden & Glas | 2010 | Advanced CAT, Fisher Information item selection |
| [5] | *Intelligent Tutoring Systems: Evolutions in Design* | Polson & Richardson | 1988 | Nguồn gốc Knowledge Tracing |

### Bài Báo Khoa Học

| # | Tiêu đề | Tác giả | Hội nghị/Journal | Link |
|---|---|---|---|---|
| [6] | *Knowledge Tracing: Modeling the Acquisition of Procedural Knowledge* | Corbett & Anderson | User Modeling and User-Adapted Interaction, 1994 | [doi:10.1007/BF01099821](https://doi.org/10.1007/BF01099821) |
| [7] | *Deep Knowledge Tracing* | Piech et al. | NeurIPS 2015 | [arxiv:1506.05908](https://arxiv.org/abs/1506.05908) |
| [8] | *A Survey of Knowledge Tracing* | Abdelrahman et al. | arXiv 2021 | [arxiv:2105.15106](https://arxiv.org/abs/2105.15106) |
| [9] | *Item Response Theory in Educational Assessment* | de Ayala | 2009 | — |
| [10] | *Applying IRT to Adaptive Testing: A Practical Guide* | Weiss | Applied Psychological Measurement, 1982 | [doi:10.1177/014662168200600301](https://doi.org/10.1177/014662168200600301) |
| [11] | *Optimal Item Pool Design for Adaptive Testing* | van der Linden | Journal of Educational Measurement, 2000 | — |
| [12] | *Bayesian Approaches to Knowledge Tracing* | Baker et al. | EDM 2008 | — |

### Thư Viện & Công Cụ

| # | Thư viện | Ngôn ngữ | Mục đích | Link |
|---|---|---|---|---|
| [13] | `scipy.optimize` | Python | MLE/EAP estimation cho IRT | [docs.scipy.org](https://docs.scipy.org/doc/scipy/reference/optimize.html) |
| [14] | `pyirt` | Python | IRT calibration với EM algorithm | [github.com/junchenfeng/pyirt](https://github.com/junchenfeng/pyirt) |
| [15] | `catsim` | Python | CAT simulation và testing | [github.com/douglasrizzo/catsim](https://github.com/douglasrizzo/catsim) |
| [16] | `pymc` | Python | Bayesian inference cho BKT | [www.pymc.io](https://www.pymc.io) |
| [17] | `numpy` / `scipy` | Python | Tính toán số học cốt lõi | [numpy.org](https://numpy.org) |
| [18] | `FastAPI` | Python | ML microservice framework | [fastapi.tiangolo.com](https://fastapi.tiangolo.com) |
| [19] | `Chart.js` | JavaScript | Radar chart & IRT curve visualization | [chartjs.org](https://www.chartjs.org) |
| [20] | `ltm` (R package) | R | Tham khảo kết quả calibration | [CRAN ltm](https://cran.r-project.org/web/packages/ltm/) |

### Tài Nguyên Bổ Sung

| # | Nguồn | Nội dung | Link |
|---|---|---|---|
| [21] | edX / Coursera: *Foundations of Psychometrics* | Khóa học IRT cơ bản | — |
| [22] | *The 2009 NAEP Technical Report* | Ví dụ IRT trong kiểm tra chuẩn quốc gia | [nces.ed.gov](https://nces.ed.gov/nationsreportcard/tdw/analysis/) |
| [23] | Khan Academy Engineering Blog: *Mastery Learning* | Ứng dụng BKT thực tế | [khanacademy.org/engineering](https://www.khanacademy.org/engineering) |
| [24] | Duolingo Research: *Halflife Regression* | Spaced repetition + knowledge tracing | [github.com/duolingo/halflife-regression](https://github.com/duolingo/halflife-regression) |
| [25] | *ASSIST ments Data Mining Competition* | Dataset công khai cho BKT | [sites.google.com/site/assistmentsdata](https://sites.google.com/site/assistmentsdata/) |

---

## Phụ Lục: Sơ Đồ Luồng ML

```
Ứng viên trả lời câu hỏi j
            │
            ▼
┌───────────────────────┐
│  Ghi nhận response    │
│  (question_id, answer,│
│   is_correct)         │
└──────────┬────────────┘
           │
     ┌─────┴──────┐
     │             │
     ▼             ▼
┌─────────┐   ┌──────────┐
│  IRT    │   │   BKT    │
│ Update  │   │  Update  │
│         │   │          │
│ θ_new = │   │ P(L_new) │
│ MLE({   │   │ = Bayes  │
│  prev   │   │  update  │
│  + j }) │   │ per skill│
└────┬────┘   └────┬─────┘
     │              │
     └──────┬───────┘
            │
            ▼
┌───────────────────────┐
│  Chọn câu hỏi tiếp    │
│  argmax Fisher(θ_new) │
│  (CAT mode only)      │
└──────────┬────────────┘
           │ (sau khi hoàn thành)
           ▼
┌───────────────────────┐
│  Generate Competency  │
│  Profile (7 domains)  │
│                       │
│  score_d = f(θ, BKT,  │
│             weights)  │
└───────────────────────┘
```

---

*Tài liệu này được duy trì cùng với codebase. Cập nhật lần cuối: 2026-05-15.*
