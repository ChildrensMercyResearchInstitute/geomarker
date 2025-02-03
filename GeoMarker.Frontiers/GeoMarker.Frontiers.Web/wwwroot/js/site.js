// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
$(document).ready(function () {
    $(".info-tooltip").hover(function () {
        $(this).popover('show');
        $(".popover").mouseout(function () {
            hidePopover($(this))
        });
    }).mouseout(function () {
        hidePopover($(this))
    }).popover({
        html: true, trigger: "manual", placement: 'top',
        content: function () { return $(this).data("tooltip-text") }
    })
    function hidePopover($this) {
        setTimeout(function () {
            if ($(".popover:hover").length == 0) {
                $this.popover('hide');
                $(".popover").off("mouseout");
            }
        }, 200)
    }
});