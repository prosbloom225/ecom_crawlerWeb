<%@ Page Title="" Language="C#" MasterPageFile="~/master.Master" AutoEventWireup="true" CodeBehind="tables.aspx.cs" Inherits="aspBootstrap.tables" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div id="page-wrapper">
        <div class="row">
            <div class="col-lg-12">
                <h1 class="page-header">Crawl Data </h1>
            </div>
            <!-- /.col-lg-12 -->
        </div>
        <!-- /.row -->
        <div class="row">
            <div class="col-lg-12">
                <div class="panel panel-default">
                    <div class="panel-heading">
                        Missing Images
                    </div>
                    <!-- /.panel-heading -->
                    <div class="well">
                        <h4>Missing Images</h4>
                        <p>Please select a crawl ID and hit Go! </p>
                        <asp:DropDownList ID="ddlCrawlID" runat="server"></asp:DropDownList>
                        <asp:Button runat="server" OnClick="updateTable" Text="Go!" />
                    </div>

                    <div class="panel-body">
                        <div class="table-responsive">
                            <table class="table table-striped table-bordered table-hover" id="tblMissingImages">
                                <thead>
                                    <tr>
                                        <th>ProductId</th>
                                        <th>Dept</th>
                                        <th>ImageName</th>
                                        <th>Type</th>
                                        <th>Color</th>
                                        <th>Url</th>
                                        <th>Nav_From</th>
                                    </tr>
                                </thead>
                                <tbody></tbody>
                            </table>
                        </div>
                        <!-- /.table-responsive -->
                    </div>
                    <!-- /.panel-body -->
                </div>
                <!-- /.panel -->
            </div>
            <!-- /.col-lg-12 -->
        </div>
        <!-- /.row -->
    </div>
    <!-- /page-wrapper -->
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="footer" runat="server">
    <!-- DataTables JavaScript -->

    <!-- Disabling the built in datatables source
    <script src="js/plugins/dataTables/jquery.dataTables.js"></script>
    <script src="js/plugins/dataTables/dataTables.bootstrap.js"></script> -->

    <script src="//cdn.datatables.net/1.10.4/js/jquery.dataTables.min.js"></script>
    <script src="//cdn.datatables.net/plug-ins/3cfcc339e89/integration/bootstrap/3/dataTables.bootstrap.js"></script>
    <link href="//cdn.datatables.net/plug-ins/3cfcc339e89/integration/bootstrap/3/dataTables.bootstrap.css" rel="stylesheet" type="text/css" />

    <script src="//cdn.datatables.net/tabletools/2.2.3/js/dataTables.tableTools.min.js"></script>
    <link href="//cdn.datatables.net/tabletools/2.2.3/css/dataTables.tableTools.css" rel="stylesheet" type="text/css" />

    <link href="//cdn.datatables.net/responsive/1.0.3/css/dataTables.responsive.css" rel="stylesheet" type="text/css" />
    <script src="//cdn.datatables.net/responsive/1.0.3/js/dataTables.responsive.js"></script>

    <script type="text/javascript">
        function table(a) {
            $('#tblMissingImages').dataTable({
                'processing': true,
                'serverSide': false,
                'responsive': true,
                'ajax': {
                    'url': '/missingImageTableHandler.ashx',
                    'dataSrc': 'aData',
                    'data': String(a),
                },
                "dom": 'T<"clear">lfrtip',
                "tableTools": {
                    "sSwfPath": "/js/plugins/dataTables/copy_csv_xls.swf",
                    "aButtons": [
                        "copy", "csv", "xls"
                    ]

                },
                "columns": [
                    { "width": "10%" },
                    null,
                    null,
                    null,
                    null,
                    null,
                    null
                ]
            });
        };
    </script>
</asp:Content>
